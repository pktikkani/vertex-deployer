using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WSMDeployer.Models;
using WSMDeployer.Services;

namespace WSMDeployer.ViewModels
{
    public class LogsPageViewModel : ViewModelBase
    {
        private readonly DatabaseService _db;
        private ObservableCollection<Deployment> _deployments = new ObservableCollection<Deployment>();
        private Deployment? _selectedDeployment;
        private string _searchText = string.Empty;
        private string _filterStatus = "All";
        private bool _isLoading = false;

        public LogsPageViewModel(DatabaseService db)
        {
            _db = db;

            RefreshCommand = new RelayCommand(async () => await LoadDeployments());
            ClearLogsCommand = new RelayCommand(OnClearLogs, CanClearLogs);
            ExportLogsCommand = new RelayCommand(OnExportLogs, () => Deployments.Count > 0);

            // Load deployments on initialization
            _ = LoadDeployments();
        }

        public ObservableCollection<Deployment> Deployments
        {
            get => _deployments;
            set => SetProperty(ref _deployments, value);
        }

        public Deployment? SelectedDeployment
        {
            get => _selectedDeployment;
            set
            {
                if (SetProperty(ref _selectedDeployment, value))
                {
                    ((RelayCommand)ClearLogsCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadDeployments();
                }
            }
        }

        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                {
                    _ = LoadDeployments();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalDeployments => Deployments.Count;
        public int SuccessfulDeployments => Deployments.Count(d => d.Status == DeploymentStatus.Success);
        public int FailedDeployments => Deployments.Count(d => d.Status == DeploymentStatus.Failed);
        public int InProgressDeployments => Deployments.Count(d => d.Status == DeploymentStatus.InProgress);

        public ICommand RefreshCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand ExportLogsCommand { get; }

        private async Task LoadDeployments()
        {
            IsLoading = true;

            try
            {
                await Task.Run(() =>
                {
                    var allDeployments = _db.GetAllDeployments();

                    // Apply filters
                    var filtered = allDeployments.AsEnumerable();

                    // Filter by status
                    if (FilterStatus != "All")
                    {
                        var status = Enum.Parse<DeploymentStatus>(FilterStatus);
                        filtered = filtered.Where(d => d.Status == status);
                    }

                    // Filter by search text
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var search = SearchText.ToLower();
                        filtered = filtered.Where(d =>
                            d.TargetHostname.ToLower().Contains(search) ||
                            d.Method.ToLower().Contains(search) ||
                            (d.ErrorMessage != null && d.ErrorMessage.ToLower().Contains(search))
                        );
                    }

                    Deployments.Clear();
                    foreach (var deployment in filtered.OrderByDescending(d => d.StartTime))
                    {
                        Deployments.Add(deployment);
                    }
                });

                OnPropertyChanged(nameof(TotalDeployments));
                OnPropertyChanged(nameof(SuccessfulDeployments));
                OnPropertyChanged(nameof(FailedDeployments));
                OnPropertyChanged(nameof(InProgressDeployments));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanClearLogs()
        {
            return Deployments.Count > 0;
        }

        private async void OnClearLogs()
        {
            // TODO: Add confirmation dialog
            await Task.Run(() =>
            {
                _db.ClearDeploymentHistory();
            });

            await LoadDeployments();
        }

        private async void OnExportLogs()
        {
            // TODO: Implement export functionality
            await Task.CompletedTask;
        }
    }
}
