using System;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Interfaces
{
    /// <summary>
    /// Provides centralized error handling functionality for the application.
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Executes an operation with centralized error handling.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging purposes.</param>
        /// <param name="showDialogOnError">Whether to show a dialog to the user if an error occurs.</param>
        /// <returns>The result of the operation, or default(T) if an error occurred.</returns>
        Task<T?> TryExecuteAsync<T>(Func<Task<T>> operation, string operationName, bool showDialogOnError = true);

        /// <summary>
        /// Executes an operation that doesn't return a result with centralized error handling.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging purposes.</param>
        /// <param name="showDialogOnError">Whether to show a dialog to the user if an error occurs.</param>
        /// <returns>true if the operation completed successfully; otherwise, false.</returns>
        Task<bool> TryExecuteAsync(Func<Task> operation, string operationName, bool showDialogOnError = true);

        /// <summary>
        /// Handles an exception that has already occurred.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="operationName">The name of the operation that threw the exception.</param>
        /// <param name="showDialogOnError">Whether to show a dialog to the user about the error.</param>
        /// <returns>A task that represents the asynchronous handle operation.</returns>
        Task HandleExceptionAsync(Exception exception, string operationName, bool showDialogOnError = true);

        /// <summary>
        /// Shows an error dialog to the user.
        /// </summary>
        /// <param name="title">The title of the error dialog.</param>
        /// <param name="message">The main error message.</param>
        /// <param name="details">Additional details about the error.</param>
        /// <returns>A task that represents the asynchronous show operation.</returns>
        Task ShowErrorDialogAsync(string title, string message, string? details = null);

        /// <summary>
        /// Gets a user-friendly error message for an exception.
        /// </summary>
        /// <param name="exception">The exception to get a message for.</param>
        /// <returns>A user-friendly error message.</returns>
        string GetUserFriendlyErrorMessage(Exception exception);

        /// <summary>
        /// Gets a suggested action for the user to take based on an exception.
        /// </summary>
        /// <param name="exception">The exception to get a suggestion for.</param>
        /// <returns>A suggested action for the user.</returns>
        string GetSuggestedUserAction(Exception exception);

        /// <summary>
        /// Logs an error for diagnostic purposes.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="operationName">The name of the operation that threw the exception.</param>
        /// <param name="additionalInfo">Any additional information to include in the log.</param>
        void LogError(Exception exception, string operationName, string? additionalInfo = null);

        /// <summary>
        /// Adds global error handlers to catch unhandled exceptions.
        /// </summary>
        void RegisterGlobalErrorHandlers();

        /// <summary>
        /// Creates a detailed error report for troubleshooting.
        /// </summary>
        /// <param name="exception">The exception to create a report for.</param>
        /// <param name="operationName">The name of the operation that threw the exception.</param>
        /// <returns>A detailed error report.</returns>
        string CreateErrorReport(Exception exception, string operationName);

        /// <summary>
        /// Saves an error report to a file.
        /// </summary>
        /// <param name="report">The error report to save.</param>
        /// <param name="filePath">The file path to save the report to, or null to use a default location.</param>
        /// <returns>The path to the saved error report file.</returns>
        Task<string> SaveErrorReportAsync(string report, string? filePath = null);

        /// <summary>
        /// Gets the exception diagnostic data for the specified exception.
        /// </summary>
        /// <param name="exception">The exception to get data for.</param>
        /// <returns>A dictionary of diagnostic data.</returns>
        ErrorDiagnosticData GetExceptionDiagnosticData(Exception exception);
    }

    /// <summary>
    /// Contains diagnostic data about an error.
    /// </summary>
    public class ErrorDiagnosticData
    {
        /// <summary>
        /// Gets or sets the type of the exception.
        /// </summary>
        public string ExceptionType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exception stack trace.
        /// </summary>
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source of the exception.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the exception occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the inner exception diagnostic data, if any.
        /// </summary>
        public ErrorDiagnosticData? InnerException { get; set; }

        /// <summary>
        /// Gets or sets the target site of the exception.
        /// </summary>
        public string TargetSite { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string AppVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operating system information.
        /// </summary>
        public string OsInfo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the available memory at the time of the exception.
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// Gets or sets the total memory at the time of the exception.
        /// </summary>
        public long TotalMemory { get; set; }

        /// <summary>
        /// Gets or sets any additional data about the exception.
        /// </summary>
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
    }
}
