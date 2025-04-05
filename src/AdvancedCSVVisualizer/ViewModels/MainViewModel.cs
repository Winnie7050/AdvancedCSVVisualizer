using AdvancedCSVVisualizer.Core.Interfaces;
using AdvancedCSVVisualizer.Core.Utilities;
using AdvancedCSVVisualizer.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AdvancedCSVVisualizer.ViewModels
{
    /// <summary>
    /// Main view model for the application.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly LineChartViewModel _lineChartViewModel;
        private readonly BarChartViewModel _barChartViewModel;
        private readonly PieChartViewModel _pieChartViewModel;
        private readonly DivergingBarChartViewModel _divergingBarChartViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        // Navigation State
        [ObservableProperty]
        private bool _isDashboardSelected = true;
        
        [ObservableProperty]
        private bool _isLineChartSelected;
        
        [ObservableProperty]
        private bool _isBarChartSelected;
        
        [ObservableProperty]
        private bool _isPieChartSelected;
        
        [ObservableProperty]
        private bool _isDivergingBarSelected;
        
        [ObservableProperty]
        private bool _isSettingsSelected;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private string _versionInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="errorHandlingService">The error handling service.</param>
        /// <param name="dashboardViewModel">The dashboard view model.</param>
        /// <param name="lineChartViewModel">The line chart view model.</param>
        /// <param name="barChartViewModel">The bar chart view model.</param>
        /// <param name="pieChartViewModel">The pie chart view model.</param>
        /// <param name="divergingBarChartViewModel">The diverging bar chart view model.</param>
        /// <param name="settingsViewModel">The settings view model.</param>
        public MainViewModel(
            ILogger<MainViewModel> logger,
            IErrorHandlingService errorHandlingService,
            DashboardViewModel dashboardViewModel,
            LineChartViewModel lineChartViewModel,
            BarChartViewModel barChartViewModel,
            PieChartViewModel pieChartViewModel,
            DivergingBarChartViewModel divergingBarChartViewModel,
            SettingsViewModel settingsViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));
            _lineChartViewModel = lineChartViewModel ?? throw new ArgumentNullException(nameof(lineChartViewModel));
            _barChartViewModel = barChartViewModel ?? throw new ArgumentNullException(nameof(barChartViewModel));
            _pieChartViewModel = pieChartViewModel ?? throw new ArgumentNullException(nameof(pieChartViewModel));
            _divergingBarChartViewModel = divergingBarChartViewModel ?? throw new ArgumentNullException(nameof(divergingBarChartViewModel));
            _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));

            // Set version information
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionInfo = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";

            // Set initial view
            CurrentView = new DashboardView { DataContext = _dashboardViewModel };
        }

        /// <summary>
        /// Initializes the view model.
        /// </summary>
        public void Initialize()
        {
            _logger.LogInformation("Initializing MainViewModel");
            
            try
            {
                // Initialize view models
                _dashboardViewModel.Initialize();
                
                StatusMessage = "Application initialized successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing MainViewModel");
                StatusMessage = "Error initializing application";
                throw;
            }
        }

        /// <summary>
        /// Cleans up resources used by the view model.
        /// </summary>
        public void Cleanup()
        {
            _logger.LogInformation("Cleaning up MainViewModel");
            
            try
            {
                // Clean up view models
                _dashboardViewModel.Cleanup();
                _lineChartViewModel.Cleanup();
                _barChartViewModel.Cleanup();
                _pieChartViewModel.Cleanup();
                _divergingBarChartViewModel.Cleanup();
                _settingsViewModel.Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up MainViewModel");
            }
        }

        /// <summary>
        /// Navigates to the specified view.
        /// </summary>
        /// <param name="viewName">The name of the view to navigate to.</param>
        [RelayCommand]
        private void NavigateTo(string viewName)
        {
            _logger.LogInformation("Navigating to view: {ViewName}", viewName);

            try
            {
                // Reset all selection states
                IsDashboardSelected = false;
                IsLineChartSelected = false;
                IsBarChartSelected = false;
                IsPieChartSelected = false;
                IsDivergingBarSelected = false;
                IsSettingsSelected = false;

                // Set appropriate selection state and view
                switch (viewName)
                {
                    case "Dashboard":
                        IsDashboardSelected = true;
                        CurrentView = new DashboardView { DataContext = _dashboardViewModel };
                        break;
                    
                    case "LineChart":
                        IsLineChartSelected = true;
                        CurrentView = new LineChartView { DataContext = _lineChartViewModel };
                        break;
                    
                    case "BarChart":
                        IsBarChartSelected = true;
                        CurrentView = new BarChartView { DataContext = _barChartViewModel };
                        break;
                    
                    case "PieChart":
                        IsPieChartSelected = true;
                        CurrentView = new PieChartView { DataContext = _pieChartViewModel };
                        break;
                    
                    case "DivergingBar":
                        IsDivergingBarSelected = true;
                        CurrentView = new DivergingBarChartView { DataContext = _divergingBarChartViewModel };
                        break;
                    
                    case "Settings":
                        IsSettingsSelected = true;
                        CurrentView = new SettingsView { DataContext = _settingsViewModel };
                        break;
                    
                    default:
                        IsDashboardSelected = true;
                        CurrentView = new DashboardView { DataContext = _dashboardViewModel };
                        break;
                }

                StatusMessage = $"Viewing {viewName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to view: {ViewName}", viewName);
                StatusMessage = $"Error navigating to {viewName}";
                
                // Fallback to dashboard on error
                IsDashboardSelected = true;
                CurrentView = new DashboardView { DataContext = _dashboardViewModel };
            }
        }
    }
}
