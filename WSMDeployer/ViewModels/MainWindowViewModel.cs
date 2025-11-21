using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using WSMDeployer.Services;
using Avalonia;
using Avalonia.Styling;

namespace WSMDeployer.ViewModels
{
    /// <summary>
    /// Main window ViewModel - handles navigation and page management
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly ThemeService _themeService;
        private ViewModelBase _currentPage;
        private string _selectedPage = "Dashboard";
        private ThemeItem? _selectedThemeItem;

        public MainWindowViewModel()
        {
            // Initialize services
            _databaseService = new DatabaseService();
            _themeService = ThemeService.Instance;

            // Initialize ViewModels
            DashboardViewModel = new DashboardViewModel(_databaseService);
            TargetsViewModel = new TargetsViewModel(_databaseService);
            DeploymentsViewModel = new DeploymentsViewModel(_databaseService);
            LogsPageViewModel = new LogsPageViewModel(_databaseService);

            // Set initial page
            _currentPage = DashboardViewModel;

            // Load available themes
            AvailableThemes = new ObservableCollection<ThemeItem>(
                _themeService.GetAllThemes().Select(t => new ThemeItem
                {
                    Theme = t,
                    DisplayName = _themeService.GetThemeDisplayName(t)
                })
            );

            // Set initial selected theme
            _selectedThemeItem = AvailableThemes.FirstOrDefault(t => t.Theme == _themeService.CurrentTheme);

            // Initialize commands
            NavigateCommand = new RelayCommand<string>(Navigate);
            ChangeThemeCommand = new RelayCommand<AppTheme>(ChangeTheme);
        }

        // Page ViewModels
        public DashboardViewModel DashboardViewModel { get; }
        public TargetsViewModel TargetsViewModel { get; }
        public DeploymentsViewModel DeploymentsViewModel { get; }
        public LogsPageViewModel LogsPageViewModel { get; }

        // Current displayed page
        public ViewModelBase CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        // Currently selected page name for UI highlighting
        public string SelectedPage
        {
            get => _selectedPage;
            set => SetProperty(ref _selectedPage, value);
        }

        // Available themes for selector
        public ObservableCollection<ThemeItem> AvailableThemes { get; }

        // Selected theme item
        public ThemeItem? SelectedThemeItem
        {
            get => _selectedThemeItem;
            set
            {
                if (SetProperty(ref _selectedThemeItem, value) && value != null)
                {
                    ChangeTheme(value.Theme);
                }
            }
        }

        // Commands
        public ICommand NavigateCommand { get; }
        public ICommand ChangeThemeCommand { get; }

        /// <summary>
        /// Navigate to a different page
        /// </summary>
        private void Navigate(string? pageName)
        {
            if (string.IsNullOrEmpty(pageName))
                return;

            SelectedPage = pageName;

            CurrentPage = pageName switch
            {
                "Dashboard" => DashboardViewModel,
                "Targets" => TargetsViewModel,
                "Deployments" => DeploymentsViewModel,
                "Logs" => LogsPageViewModel,
                _ => DashboardViewModel
            };
        }

        /// <summary>
        /// Change application theme
        /// </summary>
        private void ChangeTheme(AppTheme theme)
        {
            _themeService.CurrentTheme = theme;
        }
    }

    /// <summary>
    /// Helper class for theme combo box items
    /// </summary>
    public class ThemeItem
    {
        public AppTheme Theme { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
