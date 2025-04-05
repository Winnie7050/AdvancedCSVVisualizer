using AdvancedCSVVisualizer.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Windows;

namespace AdvancedCSVVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="logger">The logger.</param>
        public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            DataContext = _viewModel;
            InitializeComponent();
            
            _logger.LogInformation("MainWindow initialized");
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("MainWindow loaded");
                _viewModel.Initialize();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MainWindow loading");
                MessageBox.Show(
                    $"Error initializing application: {ex.Message}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                _logger.LogInformation("MainWindow closing");
                _viewModel.Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MainWindow closing");
            }
        }
    }
}
