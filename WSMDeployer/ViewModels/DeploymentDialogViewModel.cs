using System;
using System.IO;
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

        public DeploymentDialogViewModel(DatabaseService db, Target target)
        {
            _db = db;
            _target = target;

            // Initialize commands
            BrowseMsiCommand = new RelayCommand(OnBrowseMsi);
            DeployCommand = new RelayCommand(OnDeploy, CanDeploy);
            CancelCommand = new RelayCommand(OnCancel);
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

        // Commands
        public ICommand BrowseMsiCommand { get; }
        public ICommand DeployCommand { get; }
        public ICommand CancelCommand { get; }

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
            return !string.IsNullOrWhiteSpace(MsiFilePath) &&
                   File.Exists(MsiFilePath) &&
                   !IsDeploying;
        }

        private async void OnDeploy()
        {
            IsDeploying = true;
            StatusMessage = "Starting deployment...";

            try
            {
                var deploymentService = new DeploymentService(_db);

                // Start deployment in background
                var deployment = await System.Threading.Tasks.Task.Run(async () =>
                {
                    return await deploymentService.DeployToTargetAsync(
                        _target,
                        MsiFilePath,
                        SelectedInstallScope
                    );
                });

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

        private void OnCancel()
        {
            Cancelled?.Invoke();
        }
    }
}
