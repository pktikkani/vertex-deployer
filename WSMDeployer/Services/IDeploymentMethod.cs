using System.Collections.Generic;
using System.Threading.Tasks;
using WSMDeployer.Models;

namespace WSMDeployer.Services
{
    /// <summary>
    /// Interface for different deployment methods (PowerShell, WMI, PsExec)
    /// Each method implements its own way of deploying to remote machines
    /// </summary>
    public interface IDeploymentMethod
    {
        /// <summary>
        /// Name of this deployment method
        /// </summary>
        string MethodName { get; }

        /// <summary>
        /// Priority of this method (lower number = higher priority)
        /// 1 = PowerShell Remoting (fastest, most reliable)
        /// 2 = WMI (good fallback)
        /// 3 = PsExec (last resort)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Check if this deployment method can be used for the target
        /// Tests connectivity and requirements
        /// </summary>
        Task<bool> CanDeploy(Target target, string username, string password);

        /// <summary>
        /// Deploy the MSI to the target machine
        /// </summary>
        /// <param name="target">Target machine to deploy to</param>
        /// <param name="msiPath">Local path to MSI file</param>
        /// <param name="username">Username for authentication</param>
        /// <param name="password">Password for authentication</param>
        /// <returns>Deployment result with success/failure info</returns>
        Task<DeploymentResult> Deploy(Target target, string msiPath, string username, string password);

        /// <summary>
        /// Deploy the MSI to specific user accounts on the target machine
        /// </summary>
        /// <param name="target">Target machine to deploy to</param>
        /// <param name="msiPath">Local path to MSI file</param>
        /// <param name="username">Admin username for authentication</param>
        /// <param name="password">Admin password for authentication</param>
        /// <param name="targetUsers">List of user accounts to install for</param>
        /// <returns>Deployment result with success/failure info</returns>
        Task<DeploymentResult> DeployPerUser(Target target, string msiPath, string username, string password, List<UserAccount> targetUsers);
    }
}
