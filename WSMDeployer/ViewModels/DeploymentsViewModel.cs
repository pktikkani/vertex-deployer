using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using WSMDeployer.Models;
using WSMDeployer.Services;

namespace WSMDeployer.ViewModels
{
    /// <summary>
    /// Deployments ViewModel - manages and monitors deployments
    /// </summary>
    public class DeploymentsViewModel : ViewModelBase
    {
        private readonly DatabaseService _db;
        private readonly Timer _refreshTimer;
        private Deployment? _selectedDeployment;

        public DeploymentsViewModel(DatabaseService db)
        {
            _db = db;

            Deployments = new ObservableCollection<Deployment>();

            // Initialize commands
            RefreshCommand = new RelayCommand(OnRefresh);
            ViewLogsCommand = new RelayCommand(OnViewLogs, () => SelectedDeployment != null);

            // Load initial data
            LoadDeployments();

            // Auto-refresh every 5 seconds for real-time updates
            _refreshTimer = new Timer(_ => LoadDeployments(),
                null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        // Collections
        public ObservableCollection<Deployment> Deployments { get; }

        // Selected deployment
        public Deployment? SelectedDeployment
        {
            get => _selectedDeployment;
            set
            {
                if (SetProperty(ref _selectedDeployment, value))
                {
                    ((RelayCommand)ViewLogsCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ViewLogsCommand { get; }

        // Statistics
        public int TotalDeployments => Deployments.Count;
        public int SuccessfulDeployments => Deployments.Count(d => d.Status == DeploymentStatus.Success);
        public int FailedDeployments => Deployments.Count(d => d.Status == DeploymentStatus.Failed);
        public int InProgressDeployments => Deployments.Count(d => d.Status == DeploymentStatus.InProgress);

        /// <summary>
        /// Load all deployments from database
        /// </summary>
        private void LoadDeployments()
        {
            try
            {
                var deployments = _db.GetDeploymentsForTarget(0)
                    .OrderByDescending(d => d.CreatedDate)
                    .Take(100) // Limit to last 100 deployments
                    .ToList();

                // Update on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Deployments.Clear();
                    foreach (var deployment in deployments)
                    {
                        // Load target info
                        deployment.Target = _db.GetTargetById(deployment.TargetId);
                        Deployments.Add(deployment);
                    }

                    // Update statistics
                    OnPropertyChanged(nameof(TotalDeployments));
                    OnPropertyChanged(nameof(SuccessfulDeployments));
                    OnPropertyChanged(nameof(FailedDeployments));
                    OnPropertyChanged(nameof(InProgressDeployments));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading deployments: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh deployments list
        /// </summary>
        private void OnRefresh()
        {
            LoadDeployments();
        }

        /// <summary>
        /// View logs for selected deployment
        /// </summary>
        private void OnViewLogs()
        {
            if (SelectedDeployment == null)
                return;

            // In a real implementation, this would open a log viewer dialog
            // For now, just show a message
            System.Diagnostics.Debug.WriteLine($"View logs for deployment {SelectedDeployment.Id}");
        }
    }
}
