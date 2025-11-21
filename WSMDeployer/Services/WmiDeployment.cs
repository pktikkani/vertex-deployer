using System;
using System.IO;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using WSMDeployer.Models;

namespace WSMDeployer.Services
{
    /// <summary>
    /// Deployment method using WMI (Windows Management Instrumentation)
    /// Fallback method when PowerShell Remoting is not available
    /// </summary>
    public class WmiDeployment : IDeploymentMethod
    {
        public string MethodName => "WMI";
        public int Priority => 2; // Second priority

        /// <summary>
        /// Check if WMI is available on the target
        /// </summary>
        public async Task<bool> CanDeploy(Target target, string username, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var options = new ConnectionOptions
                    {
                        Username = username,
                        Password = password,
                        Impersonation = ImpersonationLevel.Impersonate,
                        Authentication = AuthenticationLevel.PacketPrivacy,
                        Timeout = TimeSpan.FromSeconds(10)
                    };

                    var scope = new ManagementScope($"\\\\{target.Hostname}\\root\\cimv2", options);
                    scope.Connect();

                    return scope.IsConnected;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Deploy MSI using WMI
        /// </summary>
        public async Task<DeploymentResult> Deploy(Target target, string msiPath, string username, string password)
        {
            return await Task.Run(() =>
            {
                var log = new StringBuilder();
                log.AppendLine($"Starting deployment to {target.Hostname} via WMI");

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

                    // Step 2: Setup WMI connection
                    var options = new ConnectionOptions
                    {
                        Username = username,
                        Password = password,
                        Impersonation = ImpersonationLevel.Impersonate,
                        Authentication = AuthenticationLevel.PacketPrivacy
                    };

                    var scope = new ManagementScope($"\\\\{target.Hostname}\\root\\cimv2", options);
                    scope.Connect();

                    if (!scope.IsConnected)
                    {
                        return new DeploymentResult
                        {
                            Success = false,
                            Method = MethodName,
                            Error = "Failed to connect to target via WMI"
                        };
                    }

                    log.AppendLine("WMI connection established");

                    // Step 3: Copy MSI to target via SMB
                    log.AppendLine("Copying MSI to target...");
                    var remoteTempPath = $"\\\\{target.Hostname}\\C$\\Temp";

                    // Create temp directory if it doesn't exist
                    if (!Directory.Exists(remoteTempPath))
                    {
                        Directory.CreateDirectory(remoteTempPath);
                    }

                    var remoteFilePath = Path.Combine(remoteTempPath, msiFileName);
                    File.Copy(msiPath, remoteFilePath, true);
                    log.AppendLine("MSI copied successfully");

                    // Step 4: Install MSI remotely via WMI
                    log.AppendLine("Starting remote installation...");

                    var processClass = new ManagementClass(scope, new ManagementPath("Win32_Process"), null);
                    var inParams = processClass.GetMethodParameters("Create");

                    // Build msiexec command
                    var commandLine = $"msiexec.exe /i C:\\Temp\\{msiFileName} /quiet /norestart /l*v C:\\Temp\\vertex_install.log";
                    inParams["CommandLine"] = commandLine;

                    // Execute the process
                    var outParams = processClass.InvokeMethod("Create", inParams, null);

                    var returnValue = (uint)outParams["returnValue"];
                    var processId = (uint)outParams["processId"];

                    if (returnValue != 0)
                    {
                        return new DeploymentResult
                        {
                            Success = false,
                            Method = MethodName,
                            Error = $"Failed to start installation process. Return value: {returnValue}",
                            Output = log.ToString()
                        };
                    }

                    log.AppendLine($"Installation process started (PID: {processId})");

                    // Step 5: Wait for process to complete
                    log.AppendLine("Waiting for installation to complete...");
                    var completed = WaitForProcess(scope, processId, TimeSpan.FromMinutes(10));

                    if (!completed)
                    {
                        return new DeploymentResult
                        {
                            Success = false,
                            Method = MethodName,
                            Error = "Installation timeout after 10 minutes",
                            Output = log.ToString()
                        };
                    }

                    log.AppendLine("Installation process completed");

                    // Step 6: Verify installation
                    log.AppendLine("Verifying installation...");
                    var serviceExists = CheckServiceExists(scope, "VertexService");

                    if (serviceExists)
                    {
                        log.AppendLine("Vertex service detected - installation successful");
                    }
                    else
                    {
                        log.AppendLine("Warning: Vertex service not detected immediately (may start on next boot)");
                    }

                    // Step 7: Cleanup
                    log.AppendLine("Cleaning up temporary files...");
                    try
                    {
                        File.Delete(remoteFilePath);
                        log.AppendLine("Cleanup completed");
                    }
                    catch
                    {
                        log.AppendLine("Warning: Failed to delete temporary MSI file");
                    }

                    return new DeploymentResult
                    {
                        Success = true,
                        Method = MethodName,
                        Output = log.ToString()
                    };
                }
                catch (UnauthorizedAccessException)
                {
                    log.AppendLine("Access denied - check credentials and permissions");
                    return new DeploymentResult
                    {
                        Success = false,
                        Method = MethodName,
                        Error = "Access denied - invalid credentials or insufficient permissions",
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
            });
        }

        /// <summary>
        /// Wait for a process to complete
        /// </summary>
        private bool WaitForProcess(ManagementScope scope, uint processId, TimeSpan timeout)
        {
            var endTime = DateTime.Now.Add(timeout);

            while (DateTime.Now < endTime)
            {
                try
                {
                    var query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE ProcessId = {processId}");
                    using (var searcher = new ManagementObjectSearcher(scope, query))
                    {
                        var processes = searcher.Get();
                        if (processes.Count == 0)
                        {
                            // Process no longer exists - completed
                            return true;
                        }
                    }

                    System.Threading.Thread.Sleep(5000); // Check every 5 seconds
                }
                catch
                {
                    // Error checking process - assume it completed
                    return true;
                }
            }

            return false; // Timeout
        }

        /// <summary>
        /// Check if a service exists on the target
        /// </summary>
        private bool CheckServiceExists(ManagementScope scope, string serviceName)
        {
            try
            {
                var query = new ObjectQuery($"SELECT * FROM Win32_Service WHERE Name = '{serviceName}'");
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    var services = searcher.Get();
                    return services.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
