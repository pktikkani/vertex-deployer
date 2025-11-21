using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WSMDeployer.Models
{
    /// <summary>
    /// Represents a user account on a target machine
    /// </summary>
    public class UserAccount : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SID { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsEnabled { get; set; }
        public string Description { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayName => string.IsNullOrEmpty(FullName)
            ? Username
            : $"{FullName} ({Username})";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
