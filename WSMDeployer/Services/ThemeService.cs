using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WSMDeployer.Services
{
    /// <summary>
    /// Simple theme options
    /// </summary>
    public enum AppTheme
    {
        System,
        Light,
        Dark
    }

    /// <summary>
    /// Service for managing application themes
    /// </summary>
    public class ThemeService
    {
        private static ThemeService? _instance;
        private AppTheme _currentTheme = AppTheme.System;
        private readonly Dictionary<AppTheme, string> _themeResourcePaths;
        private ResourceDictionary? _loadedThemeResources;

        public static ThemeService Instance => _instance ??= new ThemeService();

        private ThemeService()
        {
            // Map theme enum to resource paths
            _themeResourcePaths = new Dictionary<AppTheme, string>
            {
                { AppTheme.Light, "/Styles/Themes/Light.axaml" },
                { AppTheme.Dark, "/Styles/Themes/Dark.axaml" }
            };
        }

        public event EventHandler<AppTheme>? ThemeChanged;

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ApplyTheme(value);
                    ThemeChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Get all available themes
        /// </summary>
        public IEnumerable<AppTheme> GetAllThemes()
        {
            return new[] { AppTheme.System, AppTheme.Light, AppTheme.Dark };
        }

        /// <summary>
        /// Get friendly display name for a theme
        /// </summary>
        public string GetThemeDisplayName(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.System => "System (Auto)",
                AppTheme.Light => "Light",
                AppTheme.Dark => "Dark",
                _ => theme.ToString()
            };
        }

        private void ApplyTheme(AppTheme theme)
        {
            if (Application.Current == null) return;

            var app = Application.Current;

            // For System theme, detect OS preference
            if (theme == AppTheme.System)
            {
                // Let Avalonia decide based on system settings
                app.RequestedThemeVariant = ThemeVariant.Default;

                // Load appropriate theme based on actual theme
                var actualTheme = app.ActualThemeVariant == ThemeVariant.Dark ? AppTheme.Dark : AppTheme.Light;
                LoadThemeResources(actualTheme);
            }
            else
            {
                // Set explicit theme
                app.RequestedThemeVariant = theme == AppTheme.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
                LoadThemeResources(theme);
            }

            System.Diagnostics.Debug.WriteLine($"Applied theme: {GetThemeDisplayName(theme)}");
        }

        private void LoadThemeResources(AppTheme theme)
        {
            if (Application.Current == null || theme == AppTheme.System) return;

            var app = Application.Current;

            if (_themeResourcePaths.TryGetValue(theme, out var resourcePath))
            {
                try
                {
                    // Load the theme resource dictionary
                    var uri = new Uri($"avares://WSMDeployer{resourcePath}");
                    var themeResources = (ResourceDictionary)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(uri);

                    // Remove the previously loaded theme resources if any
                    if (_loadedThemeResources != null)
                    {
                        app.Resources.MergedDictionaries.Remove(_loadedThemeResources);
                    }

                    // Add the new theme resources
                    app.Resources.MergedDictionaries.Add(themeResources);
                    _loadedThemeResources = themeResources;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load theme: {ex.Message}");
                }
            }
        }

        public void Initialize()
        {
            // Apply initial theme
            ApplyTheme(_currentTheme);
        }
    }
}
