using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using WSMDeployer.Models;
using WSMDeployer.Services;

namespace WSMDeployer.ViewModels
{
    /// <summary>
    /// ViewModel for the deployment dialog
    /// </summary>
    public class DeploymentDialogViewModel : ViewModelBase
    {
        private readonly DatabaseService _db;
        private readonly Target _target;
        private string _msiFilePath = string.Empty;
        private InstallScope _selectedInstallScope = InstallScope.SystemWide;
        private bool _isDeploying = false;
        private string _statusMessage = string.Empty;
        private bool _isLoadingUsers = false;
        private ObservableCollection<UserAccount> _availableUsers = new ObservableCollection<UserAccount>();
        private string _adminUsername = string.Empty;
        private string _adminPassword = string.Empty;
        private ObservableCollection<string> _logMessages = new ObservableCollection<string>();

        public DeploymentDialogViewModel(DatabaseService db, Target target)
        {
            _db = db;
            _target = target;

            // Initialize commands
            BrowseMsiCommand = new RelayCommand(OnBrowseMsi);
            DeployCommand = new RelayCommand(OnDeploy, CanDeploy);
            CancelCommand = new RelayCommand(OnCancel);
            LoadUsersCommand = new RelayCommand(OnLoadUsers, CanLoadUsers);
        }

        public event Action? DeploymentStarted;
        public event Action? Cancelled;

        public string TargetHostname => _target.Hostname;
        public string TargetIp => _target.IPAddress ?? "N/A";

        public string MsiFilePath
        {
            get => _msiFilePath;
            set
            {
                if (SetProperty(ref _msiFilePath, value))
                {
                    ((RelayCommand)DeployCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public InstallScope SelectedInstallScope
        {
            get => _selectedInstallScope;
            set => SetProperty(ref _selectedInstallScope, value);
        }

        public bool IsSystemWide
        {
            get => _selectedInstallScope == InstallScope.SystemWide;
            set
            {
                if (value)
                {
                    SelectedInstallScope = InstallScope.SystemWide;
                }
            }
        }

        public bool IsPerUser
        {
            get => _selectedInstallScope == InstallScope.PerUser;
            set
            {
                if (value)
                {
                    SelectedInstallScope = InstallScope.PerUser;
                    OnPropertyChanged(nameof(IsPerUser));
                    OnPropertyChanged(nameof(ShowUserSelection));
                    ((RelayCommand)LoadUsersCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsDeploying
        {
            get => _isDeploying;
            set => SetProperty(ref _isDeploying, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsLoadingUsers
        {
            get => _isLoadingUsers;
            set
            {
                if (SetProperty(ref _isLoadingUsers, value))
                {
                    OnPropertyChanged(nameof(LoadUsersButtonText));
                }
            }
        }

        public ObservableCollection<UserAccount> AvailableUsers
        {
            get => _availableUsers;
            set => SetProperty(ref _availableUsers, value);
        }

        public bool ShowUserSelection => IsPerUser;

        public bool HasUsers => AvailableUsers.Count > 0;

        public int SelectedUserCount => AvailableUsers.Count(u => u.IsSelected);

        public string LoadUsersButtonText => IsLoadingUsers ? "Loading Users..." : "Fetch Users from Target";

        public string AdminUsername
        {
            get => _adminUsername;
            set
            {
                if (SetProperty(ref _adminUsername, value))
                {
                    ((RelayCommand)DeployCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)LoadUsersCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string AdminPassword
        {
            get => _adminPassword;
            set
            {
                if (SetProperty(ref _adminPassword, value))
                {
                    ((RelayCommand)DeployCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)LoadUsersCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<string> LogMessages
        {
            get => _logMessages;
            set => SetProperty(ref _logMessages, value);
        }

        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogMessages.Insert(0, $"[{timestamp}] {message}");
        }

        // Commands
        public ICommand BrowseMsiCommand { get; }
        public ICommand DeployCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand LoadUsersCommand { get; }

        private async void OnBrowseMsi()
        {
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow != null)
            {
                var dialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Select Vertex MSI File",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("MSI Installer")
                        {
                            Patterns = new[] { "*.msi" }
                        },
                        new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                        {
                            Patterns = new[] { "*.*" }
                        }
                    }
                };

                var result = await mainWindow.StorageProvider.OpenFilePickerAsync(dialog);

                if (result.Count > 0)
                {
                    MsiFilePath = result[0].Path.LocalPath;
                }
            }
        }

        private bool CanDeploy()
        {
            // Require credentials
            if (string.IsNullOrWhiteSpace(AdminUsername) || string.IsNullOrWhiteSpace(AdminPassword))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(MsiFilePath) || !File.Exists(MsiFilePath) || IsDeploying)
            {
                return false;
            }

            // If per-user installation, require at least one user selected
            if (IsPerUser)
            {
                return SelectedUserCount > 0;
            }

            return true;
        }

        private async void OnDeploy()
        {
            IsDeploying = true;
            StatusMessage = "Starting deployment...";

            try
            {
                var deploymentService = new DeploymentService(_db);

                Deployment deployment;

                if (IsPerUser && AvailableUsers.Count > 0)
                {
                    // Per-user deployment with selected users
                    var selectedUsers = AvailableUsers.Where(u => u.IsSelected).ToList();

                    if (selectedUsers.Count == 0)
                    {
                        StatusMessage = "Please select at least one user";
                        IsDeploying = false;
                        return;
                    }

                    StatusMessage = $"Deploying to {selectedUsers.Count} user(s)...";

                    // Start per-user deployment in background with provided credentials
                    deployment = await System.Threading.Tasks.Task.Run(async () =>
                    {
                        return await deploymentService.DeployToTargetPerUser(
                            _target,
                            MsiFilePath,
                            AdminUsername,
                            AdminPassword,
                            selectedUsers
                        );
                    });
                }
                else
                {
                    // System-wide deployment
                    StatusMessage = "Deploying system-wide...";

                    deployment = await System.Threading.Tasks.Task.Run(async () =>
                    {
                        return await deploymentService.DeployToTargetAsync(
                            _target,
                            MsiFilePath,
                            AdminUsername,
                            AdminPassword,
                            SelectedInstallScope
                        );
                    });
                }

                if (deployment.Status == DeploymentStatus.Success)
                {
                    StatusMessage = "Deployment completed successfully!";
                }
                else
                {
                    StatusMessage = $"Deployment failed: {deployment.ErrorMessage}";
                }

                // Notify that deployment started
                DeploymentStarted?.Invoke();

                // Close dialog after a brief delay to show result
                await System.Threading.Tasks.Task.Delay(2000);
                OnCancel();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Deployment error: {ex.Message}";
                IsDeploying = false;
            }
        }

        private bool CanLoadUsers()
        {
            // Require credentials before loading users
            return IsPerUser && !IsLoadingUsers && !IsDeploying &&
                   !string.IsNullOrWhiteSpace(AdminUsername) &&
                   !string.IsNullOrWhiteSpace(AdminPassword);
        }

        private async void OnLoadUsers()
        {
            IsLoadingUsers = true;
            StatusMessage = "Loading users from target machine...";
            AddLog($"Loading users from {_target.Hostname}...");
            AddLog($"Using credentials: {AdminUsername}");

            try
            {
                var userService = new UserManagementService();

                // Load users in background using provided credentials
                AddLog("Connecting to target via WMI...");
                var users = await userService.GetNonAdminUsers(_target, AdminUsername, AdminPassword);

                AddLog($"Successfully retrieved {users.Count} non-admin user(s)");

                AvailableUsers.Clear();
                foreach (var user in users)
                {
                    AddLog($"  - {user.DisplayName} (Enabled: {user.IsEnabled})");
                    user.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(UserAccount.IsSelected))
                        {
                            OnPropertyChanged(nameof(SelectedUserCount));
                            ((RelayCommand)DeployCommand).RaiseCanExecuteChanged();
                        }
                    };
                    AvailableUsers.Add(user);
                }

                OnPropertyChanged(nameof(HasUsers));
                OnPropertyChanged(nameof(SelectedUserCount));
                ((RelayCommand)DeployCommand).RaiseCanExecuteChanged();

                if (users.Count == 0)
                {
                    StatusMessage = "No non-admin users found on target machine";
                    AddLog("WARNING: No non-admin users found on target machine");
                }
                else
                {
                    StatusMessage = $"Found {users.Count} non-admin user(s)";
                    AddLog($"SUCCESS: Loaded {users.Count} user(s) successfully");
                }

                // Clear status after a delay
                await System.Threading.Tasks.Task.Delay(3000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load users: {ex.Message}";
                AddLog($"ERROR: Failed to load users - {ex.Message}");
                AddLog($"Stack trace: {ex.StackTrace}");
                await System.Threading.Tasks.Task.Delay(3000);
                StatusMessage = string.Empty;
            }
            finally
            {
                IsLoadingUsers = false;
                ((RelayCommand)LoadUsersCommand).RaiseCanExecuteChanged();
            }
        }

        private void OnCancel()
        {
            Cancelled?.Invoke();
        }
    }
}
