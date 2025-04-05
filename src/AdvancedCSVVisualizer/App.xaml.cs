using AdvancedCSVVisualizer.Core.Interfaces;
using AdvancedCSVVisualizer.Core.Services;
using AdvancedCSVVisualizer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace AdvancedCSVVisualizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            // Setup unhandled exception handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            // Register error handling
            var errorHandlingService = _serviceProvider.GetRequiredService<IErrorHandlingService>();
            errorHandlingService.RegisterGlobalErrorHandlers();

            // Get the logger
            var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Application starting up");

            // Show the main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Configure logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddNLog(new NLogProviderOptions
                {
                    CaptureMessageTemplates = true,
                    CaptureMessageProperties = true
                });
            });

            // Configure services
            ConfigureCoreServices(services);
            ConfigureViewModels(services);
            ConfigureViews(services);
        }

        private void ConfigureCoreServices(ServiceCollection services)
        {
            // Register core services
            services.AddSingleton<IFileSystemService, FileSystemService>();
            services.AddSingleton<ICSVParserService, CSVParserService>();
            services.AddSingleton<IErrorHandlingService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ErrorHandlingService>>();
                var fileSystemService = provider.GetRequiredService<IFileSystemService>();

                return new ErrorHandlingService(
                    logger,
                    fileSystemService,
                    async (title, message, details) =>
                    {
                        // UI callback for error dialogs
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show(
                                $"{message}\n\n{details}",
                                title,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        });
                    });
            });

            // Register cache service with default cache directory
            var cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AdvancedCSVVisualizer", "Cache");

            services.AddSingleton<ICacheService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<CacheService>>();
                var fileSystemService = provider.GetRequiredService<IFileSystemService>();

                return new CacheService(logger, fileSystemService, cacheDirectory);
            });

            // Register other services
            services.AddSingleton<IDataProcessingService, DataProcessingService>();
            services.AddSingleton<IVisualizationService, VisualizationService>();
        }

        private void ConfigureViewModels(ServiceCollection services)
        {
            // Register view models
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddTransient<LineChartViewModel>();
            services.AddTransient<BarChartViewModel>();
            services.AddTransient<PieChartViewModel>();
            services.AddTransient<DivergingBarChartViewModel>();
            services.AddTransient<SettingsViewModel>();
        }

        private void ConfigureViews(ServiceCollection services)
        {
            // Register views
            services.AddSingleton<MainWindow>();
            services.AddTransient<Views.DashboardView>();
            services.AddTransient<Views.LineChartView>();
            services.AddTransient<Views.BarChartView>();
            services.AddTransient<Views.PieChartView>();
            services.AddTransient<Views.DivergingBarChartView>();
            services.AddTransient<Views.SettingsView>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up services
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUnhandledException(e.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
            e.Handled = true;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            try
            {
                // Try to get the error handling service
                var errorHandlingService = _serviceProvider?.GetService<IErrorHandlingService>();
                if (errorHandlingService != null)
                {
                    // Log the error using the service
                    errorHandlingService.HandleExceptionAsync(exception, source).Wait();
                }
                else
                {
                    // Fallback if service is not available
                    var assemblyName = Assembly.GetExecutingAssembly().GetName();
                    var logDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        assemblyName.Name, "Logs");

                    Directory.CreateDirectory(logDirectory);

                    var logFile = Path.Combine(logDirectory,
                        $"Error_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                    var errorMessage = $"Unhandled exception ({source}):\n{exception}";
                    File.WriteAllText(logFile, errorMessage);

                    MessageBox.Show(
                        $"An unhandled exception occurred. The application may be in an unstable state.\n\nError details have been saved to:\n{logFile}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch
            {
                // Last resort error handling
                MessageBox.Show(
                    "A critical error occurred and could not be properly logged.\n\n" +
                    "Please restart the application.",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
