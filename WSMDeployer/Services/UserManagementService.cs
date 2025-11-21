using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using WSMDeployer.Models;

namespace WSMDeployer.Services
{
    /// <summary>
    /// Service for managing and querying user accounts on remote machines
    /// </summary>
    public class UserManagementService
    {
        /// <summary>
        /// Get all non-administrator user accounts from a target machine
        /// </summary>
        public async Task<List<UserAccount>> GetNonAdminUsers(Target target, string username, string password)
        {
            return await Task.Run(() =>
            {
                var users = new List<UserAccount>();

                try
                {
                    // Check if this is a local connection
                    var isLocal = target.Hostname.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                                  target.Hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                                  target.Hostname.Equals("127.0.0.1") ||
                                  target.Hostname.Equals(".");

                    ManagementScope scope;

                    if (isLocal)
                    {
                        // Local connection - don't use credentials
                        System.Diagnostics.Debug.WriteLine("Local connection detected - using current user context");
                        scope = new ManagementScope($"\\\\{target.Hostname}\\root\\cimv2");
                        scope.Connect();
                    }
                    else
                    {
                        // Remote connection - use provided credentials
                        System.Diagnostics.Debug.WriteLine($"Remote connection to {target.Hostname} - using credentials: {username}");
                        var options = new ConnectionOptions
                        {
                            Username = username,
                            Password = password,
                            Impersonation = ImpersonationLevel.Impersonate,
                            Authentication = AuthenticationLevel.PacketPrivacy
                        };

                        scope = new ManagementScope($"\\\\{target.Hostname}\\root\\cimv2", options);
                        scope.Connect();
                    }

                    if (!scope.IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine("WMI connection failed");
                        return users;
                    }

                    // Query all local user accounts
                    var query = new ObjectQuery("SELECT * FROM Win32_UserAccount WHERE LocalAccount = True");
                    using (var searcher = new ManagementObjectSearcher(scope, query))
                    using (var results = searcher.Get())
                    {
                        var totalUsers = 0;
                        foreach (ManagementObject user in results)
                        {
                            totalUsers++;
                            var userName = user["Name"]?.ToString() ?? string.Empty;
                            var sid = user["SID"]?.ToString() ?? string.Empty;
                            var fullName = user["FullName"]?.ToString() ?? string.Empty;
                            var description = user["Description"]?.ToString() ?? string.Empty;
                            var disabled = user["Disabled"] != null && (bool)user["Disabled"];

                            System.Diagnostics.Debug.WriteLine($"Found user: {userName}, Disabled: {disabled}");

                            // Check if user is an administrator
                            bool isAdmin = IsUserAdministrator(scope, userName);
                            System.Diagnostics.Debug.WriteLine($"  IsAdmin: {isAdmin}");

                            // Include ALL users for now - let UI decide filtering
                            users.Add(new UserAccount
                            {
                                Username = userName,
                                FullName = fullName,
                                SID = sid,
                                Description = description,
                                IsAdmin = isAdmin,
                                IsEnabled = !disabled,
                                IsSelected = false
                            });
                        }
                        System.Diagnostics.Debug.WriteLine($"Total local users found: {totalUsers}, After filtering: {users.Count}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error querying users: {ex.Message}");
                }

                return users.OrderBy(u => u.Username).ToList();
            });
        }

        /// <summary>
        /// Check if a user is a member of the Administrators group
        /// </summary>
        private bool IsUserAdministrator(ManagementScope scope, string username)
        {
            try
            {
                // Query group membership for Administrators group
                var query = new ObjectQuery(
                    "SELECT * FROM Win32_GroupUser WHERE " +
                    $"GroupComponent=\"Win32_Group.Domain='{Environment.MachineName}',Name='Administrators'\""
                );

                using (var searcher = new ManagementObjectSearcher(scope, query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject result in results)
                    {
                        var partComponent = result["PartComponent"]?.ToString();
                        if (partComponent != null && partComponent.Contains($"Name=\"{username}\""))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // If we can't determine, assume not admin for safety
                return false;
            }

            return false;
        }

        /// <summary>
        /// Get all user accounts (including admins) for debugging
        /// </summary>
        public async Task<List<UserAccount>> GetAllUsers(Target target, string username, string password)
        {
            return await Task.Run(() =>
            {
                var users = new List<UserAccount>();

                try
                {
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
                        return users;
                    }

                    var query = new ObjectQuery("SELECT * FROM Win32_UserAccount WHERE LocalAccount = True");
                    using (var searcher = new ManagementObjectSearcher(scope, query))
                    using (var results = searcher.Get())
                    {
                        foreach (ManagementObject user in results)
                        {
                            var userName = user["Name"]?.ToString() ?? string.Empty;
                            var sid = user["SID"]?.ToString() ?? string.Empty;
                            var fullName = user["FullName"]?.ToString() ?? string.Empty;
                            var description = user["Description"]?.ToString() ?? string.Empty;
                            var disabled = user["Disabled"] != null && (bool)user["Disabled"];

                            bool isAdmin = IsUserAdministrator(scope, userName);

                            users.Add(new UserAccount
                            {
                                Username = userName,
                                FullName = fullName,
                                SID = sid,
                                Description = description,
                                IsAdmin = isAdmin,
                                IsEnabled = !disabled,
                                IsSelected = false
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error querying all users: {ex.Message}");
                }

                return users.OrderBy(u => u.Username).ToList();
            });
        }
    }
}
