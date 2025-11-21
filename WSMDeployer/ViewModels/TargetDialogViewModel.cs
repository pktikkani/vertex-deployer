using System.Collections.ObjectModel;
using System.Windows.Input;
using WSMDeployer.Models;
using WSMDeployer.Services;

namespace WSMDeployer.ViewModels
{
    /// <summary>
    /// ViewModel for Add/Edit Target dialog
    /// </summary>
    public class TargetDialogViewModel : ViewModelBase
    {
        private readonly DatabaseService _db;
        private readonly Target? _existingTarget;
        private string _hostname = string.Empty;
        private string _ipAddress = string.Empty;
        private TargetType _selectedType = TargetType.Domain;
        private int? _selectedCredentialProfileId;

        public TargetDialogViewModel(DatabaseService db, Target? existingTarget = null)
        {
            _db = db;
            _existingTarget = existingTarget;

            // Load credential profiles
            CredentialProfiles = new ObservableCollection<CredentialProfile>(_db.GetAllCredentialProfiles());

            // If editing existing target, populate fields
            if (_existingTarget != null)
            {
                Hostname = _existingTarget.Hostname;
                IPAddress = _existingTarget.IPAddress ?? string.Empty;
                SelectedType = _existingTarget.Type;
                SelectedCredentialProfileId = _existingTarget.CredentialProfileId;
                IsEditMode = true;
            }

            // Commands
            SaveCommand = new RelayCommand(OnSave, CanSave);
            CancelCommand = new RelayCommand(OnCancel);
        }

        // Properties
        public bool IsEditMode { get; }
        public string DialogTitle => IsEditMode ? "Edit Target" : "Add New Target";
        public ObservableCollection<CredentialProfile> CredentialProfiles { get; }

        public string Hostname
        {
            get => _hostname;
            set
            {
                if (SetProperty(ref _hostname, value))
                {
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string IPAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public TargetType SelectedType
        {
            get => _selectedType;
            set => SetProperty(ref _selectedType, value);
        }

        public int? SelectedCredentialProfileId
        {
            get => _selectedCredentialProfileId;
            set => SetProperty(ref _selectedCredentialProfileId, value);
        }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Events
        public event System.Action? Saved;
        public event System.Action? Cancelled;

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Hostname);
        }

        private void OnSave()
        {
            if (_existingTarget != null)
            {
                // Update existing target
                _existingTarget.Hostname = Hostname;
                _existingTarget.IPAddress = string.IsNullOrWhiteSpace(IPAddress) ? null : IPAddress;
                _existingTarget.Type = SelectedType;
                _existingTarget.CredentialProfileId = SelectedCredentialProfileId;
                _existingTarget.ModifiedDate = System.DateTime.Now;

                _db.UpdateTarget(_existingTarget);
            }
            else
            {
                // Create new target
                var target = new Target
                {
                    Hostname = Hostname,
                    IPAddress = string.IsNullOrWhiteSpace(IPAddress) ? null : IPAddress,
                    Type = SelectedType,
                    Status = TargetStatus.Unknown,
                    CredentialProfileId = SelectedCredentialProfileId,
                    CreatedDate = System.DateTime.Now,
                    ModifiedDate = System.DateTime.Now
                };

                _db.AddTarget(target);
            }

            Saved?.Invoke();
        }

        private void OnCancel()
        {
            Cancelled?.Invoke();
        }
    }
}
