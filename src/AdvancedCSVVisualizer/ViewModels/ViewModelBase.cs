using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.ViewModels
{
    /// <summary>
    /// Base class for all view models in the application.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        protected readonly ILogger Logger;

        /// <summary>
        /// Gets a value indicating whether the view model is busy.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        /// <summary>
        /// Gets a value indicating whether the view model is not busy.
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Gets or sets a message indicating the current operation being performed.
        /// </summary>
        [ObservableProperty]
        private string _busyMessage = "Loading...";

        /// <summary>
        /// Gets or sets the title of the view.
        /// </summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>
        /// Gets or sets a status message for the view.
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether there's an error.
        /// </summary>
        [ObservableProperty]
        private bool _hasError;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        protected ViewModelBase(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes the view model.
        /// </summary>
        public virtual void Initialize()
        {
            Logger.LogInformation("{ViewModel} initialized", GetType().Name);
        }

        /// <summary>
        /// Cleans up resources used by the view model.
        /// </summary>
        public virtual void Cleanup()
        {
            Logger.LogInformation("{ViewModel} cleaned up", GetType().Name);
        }

        /// <summary>
        /// Executes an operation with busy state indication.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging.</param>
        /// <param name="busyMessage">A message indicating the busy state.</param>
        /// <param name="errorMessage">A message to display on error.</param>
        /// <param name="showErrorMessage">Whether to update the error message property.</param>
        /// <returns>The result of the operation, or default if an error occurred.</returns>
        protected async Task<T?> ExecuteWithBusyIndicationAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            string busyMessage = "Processing...",
            string errorMessage = "An error occurred.",
            bool showErrorMessage = true)
        {
            if (IsBusy)
                return default;

            try
            {
                IsBusy = true;
                BusyMessage = busyMessage;
                HasError = false;
                ErrorMessage = string.Empty;

                Logger.LogDebug("Starting operation: {OperationName}", operationName);
                return await operation();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in operation {OperationName}", operationName);
                
                if (showErrorMessage)
                {
                    HasError = true;
                    ErrorMessage = $"{errorMessage} {ex.Message}";
                }
                
                return default;
            }
            finally
            {
                IsBusy = false;
                Logger.LogDebug("Completed operation: {OperationName}", operationName);
            }
        }

        /// <summary>
        /// Executes an operation with busy state indication.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging.</param>
        /// <param name="busyMessage">A message indicating the busy state.</param>
        /// <param name="errorMessage">A message to display on error.</param>
        /// <param name="showErrorMessage">Whether to update the error message property.</param>
        /// <returns>true if the operation succeeded; otherwise, false.</returns>
        protected async Task<bool> ExecuteWithBusyIndicationAsync(
            Func<Task> operation,
            string operationName,
            string busyMessage = "Processing...",
            string errorMessage = "An error occurred.",
            bool showErrorMessage = true)
        {
            if (IsBusy)
                return false;

            try
            {
                IsBusy = true;
                BusyMessage = busyMessage;
                HasError = false;
                ErrorMessage = string.Empty;

                Logger.LogDebug("Starting operation: {OperationName}", operationName);
                await operation();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in operation {OperationName}", operationName);
                
                if (showErrorMessage)
                {
                    HasError = true;
                    ErrorMessage = $"{errorMessage} {ex.Message}";
                }
                
                return false;
            }
            finally
            {
                IsBusy = false;
                Logger.LogDebug("Completed operation: {OperationName}", operationName);
            }
        }

        /// <summary>
        /// Clears the error state.
        /// </summary>
        [RelayCommand]
        private void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }
}
