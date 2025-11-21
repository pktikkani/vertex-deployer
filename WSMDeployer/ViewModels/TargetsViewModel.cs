using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WSMDeployer.Models;
using WSMDeployer.Services;

namespace WSMDeployer.ViewModels
{
    /// <summary>
    /// Targets ViewModel - manages target machines
    /// </summary>
    public class TargetsViewModel : ViewModelBase
    {
        private readonly DatabaseService _db;
        private Target? _selectedTarget;
        private string _searchQuery = string.Empty;

        public TargetsViewModel(DatabaseService db)
        {
            _db = db;

            Targets = new ObservableCollection<Target>();
            CredentialProfiles = new ObservableCollection<CredentialProfile>();

            // Initialize commands
            AddTargetCommand = new RelayCommand(OnAddTarget);
            EditTargetCommand = new RelayCommand(OnEditTarget, () => SelectedTarget != null);
            DeleteTargetCommand = new RelayCommand(OnDeleteTarget, () => SelectedTarget != null);
            DeployCommand = new RelayCommand(OnDeploy, () => SelectedTarget != null);
            RefreshCommand = new RelayCommand(OnRefresh);
            SearchCommand = new RelayCommand(OnSearch);

            // Load initial data
            LoadTargets();
            LoadCredentialProfiles();
        }

        // Collections
        public ObservableCollection<Target> Targets { get; }
        public ObservableCollection<CredentialProfile> CredentialProfiles { get; }

        // Selected target
        public Target? SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                if (SetProperty(ref _selectedTarget, value))
                {
                    ((RelayCommand)EditTargetCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteTargetCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeployCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // Search query
        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        // Commands
        public ICommand AddTargetCommand { get; }
        public ICommand EditTargetCommand { get; }
        public ICommand DeleteTargetCommand { get; }
        public ICommand DeployCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }

        /// <summary>
        /// Load all targets from database
        /// </summary>
        private void LoadTargets()
        {
            var targets = _db.GetAllTargets();

            Targets.Clear();
            foreach (var target in targets)
            {
                Targets.Add(target);
            }
        }

        /// <summary>
        /// Load credential profiles
        /// </summary>
        private void LoadCredentialProfiles()
        {
            var profiles = _db.GetAllCredentialProfiles();

            CredentialProfiles.Clear();
            foreach (var profile in profiles)
            {
                CredentialProfiles.Add(profile);
            }
        }

        /// <summary>
        /// Add new target
        /// </summary>
        private async void OnAddTarget()
        {
            var dialogViewModel = new TargetDialogViewModel(_db);
            var dialog = new Views.TargetDialog(dialogViewModel);

            dialogViewModel.Saved += () =>
            {
                // Refresh the targets list
                LoadTargets();
            };

            // Show dialog
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }

        /// <summary>
        /// Edit selected target
        /// </summary>
        private async void OnEditTarget()
        {
            if (SelectedTarget == null)
                return;

            var dialogViewModel = new TargetDialogViewModel(_db, SelectedTarget);
            var dialog = new Views.TargetDialog(dialogViewModel);

            dialogViewModel.Saved += () =>
            {
                // Refresh the targets list
                LoadTargets();
            };

            // Show dialog
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }

        /// <summary>
        /// Delete selected target
        /// </summary>
        private void OnDeleteTarget()
        {
            if (SelectedTarget == null)
                return;

            _db.DeleteTarget(SelectedTarget.Id);
            Targets.Remove(SelectedTarget);
            SelectedTarget = null;
        }

        /// <summary>
        /// Refresh targets list
        /// </summary>
        private void OnRefresh()
        {
            LoadTargets();
        }

        /// <summary>
        /// Search targets
        /// </summary>
        private void OnSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                LoadTargets();
                return;
            }

            var results = _db.SearchTargets(SearchQuery);

            Targets.Clear();
            foreach (var target in results)
            {
                Targets.Add(target);
            }
        }

        /// <summary>
        /// Deploy to selected target
        /// </summary>
        private async void OnDeploy()
        {
            if (SelectedTarget == null)
                return;

            // Show deployment dialog
            var dialogViewModel = new DeploymentDialogViewModel(_db, SelectedTarget);
            var dialog = new Views.DeploymentDialog(dialogViewModel);

            dialogViewModel.DeploymentStarted += () =>
            {
                // Refresh the targets list to show updated status
                LoadTargets();
            };

            // Show dialog
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
                // Refresh after dialog closes
                LoadTargets();
            }
        }
    }
}
