using AdvancedCSVVisualizer.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Services
{
    /// <summary>
    /// Provides centralized error handling functionality for the application.
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly IFileSystemService? _fileSystemService;
        private readonly Func<string, string, string?, Task>? _showDialogFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlingService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystemService">The file system service.</param>
        /// <param name="showDialogFunc">A function to show error dialogs.</param>
        public ErrorHandlingService(
            ILogger<ErrorHandlingService> logger,
            IFileSystemService? fileSystemService = null,
            Func<string, string, string?, Task>? showDialogFunc = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystemService = fileSystemService;
            _showDialogFunc = showDialogFunc;
        }

        /// <inheritdoc/>
        public async Task<T?> TryExecuteAsync<T>(Func<Task<T>> operation, string operationName, bool showDialogOnError = true)
        {
            try
            {
                _logger.LogDebug("Executing operation: {OperationName}", operationName);
                return await operation();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, operationName, showDialogOnError);
                return default;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TryExecuteAsync(Func<Task> operation, string operationName, bool showDialogOnError = true)
        {
            try
            {
                _logger.LogDebug("Executing operation: {OperationName}", operationName);
                await operation();
                return true;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, operationName, showDialogOnError);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task HandleExceptionAsync(Exception exception, string operationName, bool showDialogOnError = true)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            // Log the error
            LogError(exception, operationName);

            // Generate an error report if necessary
            if (IsErrorReportNeeded(exception))
            {
                var report = CreateErrorReport(exception, operationName);
                await SaveErrorReportAsync(report);
            }

            // Show dialog if needed
            if (showDialogOnError && _showDialogFunc != null)
            {
                var message = GetUserFriendlyErrorMessage(exception);
                var suggestion = GetSuggestedUserAction(exception);
                
                await _showDialogFunc($"Error in {operationName}", message, suggestion);
            }
        }

        /// <inheritdoc/>
        public async Task ShowErrorDialogAsync(string title, string message, string? details = null)
        {
            if (_showDialogFunc != null)
            {
                await _showDialogFunc(title, message, details);
            }
            else
            {
                _logger.LogWarning("No dialog function provided to show error dialog. Title: {Title}, Message: {Message}", 
                    title, message);
            }
        }

        /// <inheritdoc/>
        public string GetUserFriendlyErrorMessage(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return exception switch
            {
                FileNotFoundException => "The specified file could not be found.",
                DirectoryNotFoundException => "The specified directory could not be found.",
                UnauthorizedAccessException => "You do not have permission to access the file or directory.",
                IOException io when io.Message.Contains("being used by another process") => 
                    "The file is in use by another process. Please close any other applications that may be using this file.",
                FormatException => "The data is not in the expected format.",
                OutOfMemoryException => "The application has run out of memory. Please try closing other applications or restarting.",
                NotSupportedException => "The requested operation is not supported on this file or in this context.",
                
                // Add more specific exception types as needed
                
                _ => $"An unexpected error occurred: {exception.Message}"
            };
        }

        /// <inheritdoc/>
        public string GetSuggestedUserAction(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return exception switch
            {
                FileNotFoundException => "Please verify that the file exists and try again.",
                DirectoryNotFoundException => "Please verify that the directory exists and try again.",
                UnauthorizedAccessException => "Try running the application as an administrator or contact your system administrator.",
                IOException io when io.Message.Contains("being used by another process") => 
                    "Close any other programs that might be using this file and try again.",
                FormatException => "Check that the file format is correct and try again.",
                OutOfMemoryException => "Try closing other applications to free up memory and try again.",
                NotSupportedException => "Try using a different file format or operation.",
                
                // Add more specific exception types as needed
                
                _ => "Please check the error details and try again. If the problem persists, please contact support."
            };
        }

        /// <inheritdoc/>
        public void LogError(Exception exception, string operationName, string? additionalInfo = null)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (additionalInfo != null)
            {
                _logger.LogError(exception, "Error in operation {OperationName}: {AdditionalInfo}", 
                    operationName, additionalInfo);
            }
            else
            {
                _logger.LogError(exception, "Error in operation {OperationName}", operationName);
            }
        }

        /// <inheritdoc/>
        public void RegisterGlobalErrorHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    LogError(ex, "Unhandled AppDomain Exception", $"IsTerminating: {e.IsTerminating}");
                    
                    var report = CreateErrorReport(ex, "Unhandled AppDomain Exception");
                    Task.Run(async () => await SaveErrorReportAsync(report)).Wait();
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogError(e.Exception, "Unobserved Task Exception");
                
                var report = CreateErrorReport(e.Exception, "Unobserved Task Exception");
                Task.Run(async () => await SaveErrorReportAsync(report)).Wait();
                
                e.SetObserved();
            };
        }

        /// <inheritdoc/>
        public string CreateErrorReport(Exception exception, string operationName)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var diagnosticData = GetExceptionDiagnosticData(exception);
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            
            var sb = new StringBuilder();
            
            sb.AppendLine("ERROR REPORT");
            sb.AppendLine("===========");
            sb.AppendLine();
            
            sb.AppendLine($"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Application Version: {appVersion}");
            sb.AppendLine($"Operation: {operationName}");
            sb.AppendLine();
            
            sb.AppendLine("SYSTEM INFORMATION");
            sb.AppendLine("------------------");
            sb.AppendLine($"OS: {RuntimeInformation.OSDescription}");
            sb.AppendLine($"Architecture: {RuntimeInformation.OSArchitecture}");
            sb.AppendLine($"Framework: {RuntimeInformation.FrameworkDescription}");
            sb.AppendLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
            sb.AppendLine($"Available Memory: {diagnosticData.AvailableMemory / (1024 * 1024)} MB");
            sb.AppendLine($"Total Memory: {diagnosticData.TotalMemory / (1024 * 1024)} MB");
            sb.AppendLine();
            
            sb.AppendLine("EXCEPTION DETAILS");
            sb.AppendLine("-----------------");
            sb.AppendLine($"Type: {diagnosticData.ExceptionType}");
            sb.AppendLine($"Message: {diagnosticData.Message}");
            sb.AppendLine($"Source: {diagnosticData.Source}");
            sb.AppendLine($"Target Site: {diagnosticData.TargetSite}");
            sb.AppendLine();
            
            sb.AppendLine("STACK TRACE");
            sb.AppendLine("-----------");
            sb.AppendLine(diagnosticData.StackTrace);
            sb.AppendLine();
            
            if (diagnosticData.InnerException != null)
            {
                sb.AppendLine("INNER EXCEPTION");
                sb.AppendLine("---------------");
                sb.AppendLine($"Type: {diagnosticData.InnerException.ExceptionType}");
                sb.AppendLine($"Message: {diagnosticData.InnerException.Message}");
                sb.AppendLine();
                sb.AppendLine("INNER EXCEPTION STACK TRACE");
                sb.AppendLine("--------------------------");
                sb.AppendLine(diagnosticData.InnerException.StackTrace);
                sb.AppendLine();
            }
            
            if (diagnosticData.AdditionalData.Count > 0)
            {
                sb.AppendLine("ADDITIONAL DATA");
                sb.AppendLine("---------------");
                foreach (var kvp in diagnosticData.AdditionalData)
                {
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        /// <inheritdoc/>
        public async Task<string> SaveErrorReportAsync(string report, string? filePath = null)
        {
            if (string.IsNullOrEmpty(report))
                throw new ArgumentException("Report cannot be null or empty.", nameof(report));

            try
            {
                if (_fileSystemService == null)
                {
                    _logger.LogWarning("No file system service provided to save error report.");
                    return string.Empty;
                }

                // Generate a unique filename if not provided
                if (string.IsNullOrEmpty(filePath))
                {
                    var logsDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "AdvancedCSVVisualizer", "Logs");
                    
                    await _fileSystemService.CreateDirectoryAsync(logsDirectory);
                    
                    filePath = Path.Combine(logsDirectory, 
                        $"ErrorReport_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.txt");
                }
                
                await _fileSystemService.WriteFileAsync(filePath, report);
                _logger.LogInformation("Error report saved to {FilePath}", filePath);
                
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save error report");
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public ErrorDiagnosticData GetExceptionDiagnosticData(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var process = Process.GetCurrentProcess();
            var result = new ErrorDiagnosticData
            {
                ExceptionType = exception.GetType().FullName ?? "Unknown",
                Message = exception.Message,
                StackTrace = exception.StackTrace ?? "No stack trace available",
                Source = exception.Source ?? "Unknown source",
                Timestamp = DateTime.Now,
                TargetSite = exception.TargetSite?.ToString() ?? "Unknown",
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                OsInfo = RuntimeInformation.OSDescription,
                AvailableMemory = GC.GetTotalMemory(false),
                TotalMemory = process.WorkingSet64,
                AdditionalData = new Dictionary<string, string>()
            };

            // Include inner exception if present
            if (exception.InnerException != null)
            {
                result.InnerException = new ErrorDiagnosticData
                {
                    ExceptionType = exception.InnerException.GetType().FullName ?? "Unknown",
                    Message = exception.InnerException.Message,
                    StackTrace = exception.InnerException.StackTrace ?? "No stack trace available",
                    Source = exception.InnerException.Source ?? "Unknown source",
                    Timestamp = DateTime.Now,
                    TargetSite = exception.InnerException.TargetSite?.ToString() ?? "Unknown",
                    AppVersion = result.AppVersion,
                    OsInfo = result.OsInfo,
                    AvailableMemory = result.AvailableMemory,
                    TotalMemory = result.TotalMemory
                };
            }

            // Add any data from the exception's Data property
            foreach (var key in exception.Data.Keys)
            {
                if (key != null)
                {
                    var keyString = key.ToString() ?? "Unknown Key";
                    var valueString = exception.Data[key]?.ToString() ?? "Null";
                    result.AdditionalData[keyString] = valueString;
                }
            }

            // Add specific info based on exception type
            if (exception is IOException ioException)
            {
                result.AdditionalData["HResult"] = ioException.HResult.ToString();
            }
            else if (exception is FileNotFoundException fileNotFoundException)
            {
                result.AdditionalData["FileName"] = fileNotFoundException.FileName ?? "Unknown";
                result.AdditionalData["FusionLog"] = fileNotFoundException.FusionLog ?? "None";
            }

            return result;
        }

        /// <summary>
        /// Determines whether an error report should be generated for the exception.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>true if an error report should be generated; otherwise, false.</returns>
        private bool IsErrorReportNeeded(Exception exception)
        {
            // Generate reports for all unhandled exceptions
            // Add specific rules for other exception types if needed
            return true;
        }
    }
}
