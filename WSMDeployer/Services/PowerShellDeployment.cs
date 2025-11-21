using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WSMDeployer.Models;

namespace WSMDeployer.Services
{
    /// <summary>
    /// Deployment method using PowerShell Remoting (WinRM)
    /// This is the fastest and most reliable method for domain-joined machines
    /// </summary>
    public class PowerShellDeployment : IDeploymentMethod
    {
        public string MethodName => "PowerShell Remoting";
        public int Priority => 1; // Highest priority

        /// <summary>
        /// Check if PowerShell Remoting is available on the target
        /// Tests WinRM connectivity
        /// </summary>
        public async Task<bool> CanDeploy(Target target, string username, string password)
        {
            try
            {
                var script = $@"
                    $secPassword = ConvertTo-SecureString '{password}' -AsPlainText -Force
                    $credential = New-Object System.Management.Automation.PSCredential('{username}', $secPassword)
                    Test-WSMan -ComputerName '{target.Hostname}' -Credential $credential -ErrorAction Stop
                ";

                var result = await ExecutePowerShell(script);
                return result.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deploy MSI using PowerShell Remoting
        /// </summary>
        public async Task<DeploymentResult> Deploy(Target target, string msiPath, string username, string password)
        {
            var log = new StringBuilder();
            log.AppendLine($"Starting deployment to {target.Hostname} via PowerShell Remoting");

            try
            {
                // Step 1: Validate MSI exists
                if (!File.Exists(msiPath))
                {
                    return new DeploymentResult
                    {
                        Success = false,
                        Method = MethodName,
                        Error = $"MSI file not found: {msiPath}"
                    };
                }

                log.AppendLine($"MSI file validated: {msiPath}");
                var msiFileName = Path.GetFileName(msiPath);

                // Step 2: Copy MSI to target
                log.AppendLine($"Copying MSI to target...");
                var remotePath = $"\\\\{target.Hostname}\\C$\\Temp\\{msiFileName}";

                var copyScript = $@"
                    $secPassword = ConvertTo-SecureString '{password}' -AsPlainText -Force
                    $credential = New-Object System.Management.Automation.PSCredential('{username}', $secPassword)

                    # Create temp directory if it doesn't exist
                    Invoke-Command -ComputerName '{target.Hostname}' -Credential $credential -ScriptBlock {{
                        if (-not (Test-Path 'C:\Temp')) {{
                            New-Item -ItemType Directory -Path 'C:\Temp' -Force | Out-Null
                        }}
                    }}

                    # Copy MSI file
                    Copy-Item -Path '{msiPath}' -Destination '{remotePath}' -Force
                ";

                var copyResult = await ExecutePowerShell(copyScript);
                if (copyResult.ExitCode != 0)
                {
                    return new DeploymentResult
                    {
                        Success = false,
                        Method = MethodName,
                        Error = $"Failed to copy MSI: {copyResult.Error}",
                        Output = log.ToString()
                    };
                }

                log.AppendLine("MSI copied successfully");

                // Step 3: Install MSI remotely
                log.AppendLine("Starting remote installation...");
                var installScript = $@"
                    $secPassword = ConvertTo-SecureString '{password}' -AsPlainText -Force
                    $credential = New-Object System.Management.Automation.PSCredential('{username}', $secPassword)

                    Invoke-Command -ComputerName '{target.Hostname}' -Credential $credential -ScriptBlock {{
                        param($msiPath)

                        $process = Start-Process -FilePath 'msiexec.exe' -ArgumentList '/i', $msiPath, '/quiet', '/norestart', '/l*v', 'C:\Temp\vertex_install.log' -Wait -PassThru

                        return @{{
                            ExitCode = $process.ExitCode
                            LogContent = if (Test-Path 'C:\Temp\vertex_install.log') {{ Get-Content 'C:\Temp\vertex_install.log' -Tail 20 | Out-String }} else {{ '' }}
                        }}
                    }} -ArgumentList 'C:\Temp\{msiFileName}'
                ";

                var installResult = await ExecutePowerShell(installScript);

                if (installResult.ExitCode != 0)
                {
                    log.AppendLine($"Installation failed with exit code: {installResult.ExitCode}");
                    return new DeploymentResult
                    {
                        Success = false,
                        Method = MethodName,
                        Error = $"Installation failed: {installResult.Error}",
                        Output = log.ToString()
                    };
                }

                log.AppendLine("Installation completed successfully");

                // Step 4: Verify installation
                log.AppendLine("Verifying installation...");
                var verifyScript = $@"
                    $secPassword = ConvertTo-SecureString '{password}' -AsPlainText -Force
                    $credential = New-Object System.Management.Automation.PSCredential('{username}', $secPassword)

                    Invoke-Command -ComputerName '{target.Hostname}' -Credential $credential -ScriptBlock {{
                        Get-Service -Name 'VertexService' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Status
                    }}
                ";

                var verifyResult = await ExecutePowerShell(verifyScript);
                log.AppendLine($"Service status check: {verifyResult.Output}");

                // Step 5: Cleanup
                log.AppendLine("Cleaning up temporary files...");
                var cleanupScript = $@"
                    $secPassword = ConvertTo-SecureString '{password}' -AsPlainText -Force
                    $credential = New-Object System.Management.Automation.PSCredential('{username}', $secPassword)

                    Invoke-Command -ComputerName '{target.Hostname}' -Credential $credential -ScriptBlock {{
                        Remove-Item 'C:\Temp\{msiFileName}' -Force -ErrorAction SilentlyContinue
                    }}
                ";

                await ExecutePowerShell(cleanupScript);
                log.AppendLine("Cleanup completed");

                return new DeploymentResult
                {
                    Success = true,
                    Method = MethodName,
                    Output = log.ToString()
                };
            }
            catch (Exception ex)
            {
                log.AppendLine($"Exception occurred: {ex.Message}");
                return new DeploymentResult
                {
                    Success = false,
                    Method = MethodName,
                    Error = ex.Message,
                    Output = log.ToString()
                };
            }
        }

        /// <summary>
        /// Execute a PowerShell script and return the result
        /// </summary>
        private async Task<PowerShellResult> ExecutePowerShell(string script)
        {
            return await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return new PowerShellResult
                        {
                            ExitCode = -1,
                            Error = "Failed to start PowerShell process"
                        };
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    return new PowerShellResult
                    {
                        ExitCode = process.ExitCode,
                        Output = output,
                        Error = error
                    };
                }
            });
        }

        private class PowerShellResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
        }
    }
}
