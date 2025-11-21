using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WSMDeployer.Models;

namespace WSMDeployer.Services
{
    /// <summary>
    /// Main deployment orchestrator service
    /// Coordinates the deployment process and automatically selects the best deployment method
    /// </summary>
    public class DeploymentService
    {
        private readonly DatabaseService _db;
        private readonly List<IDeploymentMethod> _deploymentMethods;

        public DeploymentService(DatabaseService db)
        {
            _db = db;

            // Initialize deployment methods in priority order
            _deploymentMethods = new List<IDeploymentMethod>
            {
                new PowerShellDeployment(),  // Priority 1
                new WmiDeployment()           // Priority 2
            };

            // Sort by priority just to be sure
            _deploymentMethods = _deploymentMethods.OrderBy(m => m.Priority).ToList();
        }

        /// <summary>
        /// Deploy to a single target with install scope option
        /// </summary>
        public async Task<Deployment> DeployToTargetAsync(
            Target target,
            string msiPath,
            InstallScope installScope)
        {
            // Get credentials from target's profile
            string username = "Administrator"; // TODO: Get from credential profile
            string password = "Password123";    // TODO: Get from credential profile

            var result = await DeployToTarget(target, msiPath, username, password, null, installScope);

            // Return the deployment record
            return new Deployment
            {
                TargetId = target.Id,
                Status = result.Success ? DeploymentStatus.Success : DeploymentStatus.Failed,
                Method = result.Method,
                ErrorMessage = result.Error,
                Log = result.Output,
                StartTime = DateTime.Now.AddMinutes(-1),
                EndTime = DateTime.Now,
                CreatedDate = DateTime.Now
            };
        }

        /// <summary>
        /// Deploy to a single target
        /// Automatically selects the best deployment method available
        /// </summary>
        public async Task<DeploymentResult> DeployToTarget(
            Target target,
            string msiPath,
            string username,
            string password,
            ConfigurationTemplate? configTemplate = null,
            InstallScope installScope = InstallScope.SystemWide)
        {
            // Create deployment record
            var deployment = new Deployment
            {
                TargetId = target.Id,
                Status = DeploymentStatus.Queued,
                CreatedDate = DateTime.Now
            };

            var deploymentId = _db.CreateDeployment(deployment);
            deployment.Id = deploymentId;

            try
            {
                // Update status to in progress
                deployment.Status = DeploymentStatus.InProgress;
                deployment.StartTime = DateTime.Now;
                _db.UpdateDeploymentStatus(deploymentId, DeploymentStatus.InProgress);

                // Update target status
                target.Status = TargetStatus.Deploying;
                _db.UpdateTarget(target);

                // Select and execute deployment method
                var result = await SelectAndDeploy(target, msiPath, username, password, installScope);

                // Update deployment record
                deployment.Method = result.Method;
                deployment.EndTime = DateTime.Now;
                deployment.Log = result.Output;

                if (result.Success)
                {
                    deployment.Status = DeploymentStatus.Success;
                    deployment.ErrorMessage = null;

                    // Update target with success
                    target.Status = TargetStatus.Online;
                    target.VertexVersion = "1.0.0"; // TODO: Get version from MSI
                    target.LastSeen = DateTime.Now;
                    target.ModifiedDate = DateTime.Now;
                }
                else
                {
                    deployment.Status = DeploymentStatus.Failed;
                    deployment.ErrorMessage = result.Error;

                    // Update target with error
                    target.Status = TargetStatus.Error;
                    target.ModifiedDate = DateTime.Now;
                }

                _db.UpdateDeploymentStatus(deploymentId, deployment.Status, deployment.ErrorMessage);
                _db.UpdateDeploymentLog(deploymentId, deployment.Log ?? string.Empty);
                _db.UpdateTarget(target);

                // Deploy configuration if provided and deployment was successful
                if (result.Success && configTemplate != null)
                {
                    await DeployConfiguration(target, configTemplate, username, password);
                }

                return result;
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                deployment.Status = DeploymentStatus.Failed;
                deployment.EndTime = DateTime.Now;
                deployment.ErrorMessage = $"Unexpected error: {ex.Message}";
                deployment.Log = ex.StackTrace;

                _db.UpdateDeploymentStatus(deploymentId, DeploymentStatus.Failed, deployment.ErrorMessage);

                target.Status = TargetStatus.Error;
                _db.UpdateTarget(target);

                return new DeploymentResult
                {
                    Success = false,
                    Method = "Unknown",
                    Error = ex.Message,
                    Output = ex.StackTrace
                };
            }
        }

        /// <summary>
        /// Deploy to multiple targets in parallel
        /// </summary>
        public async Task<List<DeploymentResult>> DeployToMultipleTargets(
            List<Target> targets,
            string msiPath,
            string username,
            string password,
            ConfigurationTemplate? configTemplate = null,
            int maxParallelDeployments = 5)
        {
            var results = new List<DeploymentResult>();
            var semaphore = new System.Threading.SemaphoreSlim(maxParallelDeployments);

            var tasks = targets.Select(async target =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await DeployToTarget(target, msiPath, username, password, configTemplate);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            results.AddRange(await Task.WhenAll(tasks));
            return results;
        }

        /// <summary>
        /// Select the best deployment method and execute deployment
        /// Tries methods in priority order until one succeeds or all fail
        /// </summary>
        private async Task<DeploymentResult> SelectAndDeploy(
            Target target,
            string msiPath,
            string username,
            string password,
            InstallScope installScope = InstallScope.SystemWide)
        {
            var errors = new List<string>();

            // Try each deployment method in priority order
            foreach (var method in _deploymentMethods)
            {
                try
                {
                    // Check if this method can deploy to the target
                    var canDeploy = await method.CanDeploy(target, username, password);

                    if (!canDeploy)
                    {
                        errors.Add($"{method.MethodName}: Not available or connectivity test failed");
                        continue;
                    }

                    // Attempt deployment
                    var result = await method.Deploy(target, msiPath, username, password);

                    if (result.Success)
                    {
                        return result;
                    }

                    // Method failed, try next one
                    errors.Add($"{method.MethodName}: {result.Error}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{method.MethodName}: Exception - {ex.Message}");
                }
            }

            // All methods failed
            return new DeploymentResult
            {
                Success = false,
                Method = "All methods failed",
                Error = "All deployment methods failed",
                Output = string.Join("\n", errors)
            };
        }

        /// <summary>
        /// Deploy configuration file to target after successful installation
        /// </summary>
        private async Task<bool> DeployConfiguration(
            Target target,
            ConfigurationTemplate configTemplate,
            string username,
            string password)
        {
            try
            {
                // TODO: Implement configuration deployment
                // For now, just return success
                await Task.CompletedTask;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Pre-flight checks before deployment
        /// Validates that deployment can proceed
        /// </summary>
        public async Task<(bool Success, string Message)> PreflightCheck(
            Target target,
            string msiPath,
            string username,
            string password)
        {
            // Check MSI file exists
            if (!System.IO.File.Exists(msiPath))
            {
                return (false, $"MSI file not found: {msiPath}");
            }

            // Check if target is already being deployed
            if (target.Status == TargetStatus.Deploying)
            {
                return (false, "Target is already being deployed to");
            }

            // Check if we can reach the target with any method
            var canReach = false;
            foreach (var method in _deploymentMethods)
            {
                if (await method.CanDeploy(target, username, password))
                {
                    canReach = true;
                    break;
                }
            }

            if (!canReach)
            {
                return (false, "Cannot reach target with any deployment method. Check connectivity and credentials.");
            }

            return (true, "Pre-flight checks passed");
        }
    }
}
