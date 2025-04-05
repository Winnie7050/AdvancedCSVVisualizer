using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AdvancedCSVVisualizer.Core.Utilities
{
    /// <summary>
    /// Provides an implementation of <see cref="ICommand"/> that can execute asynchronous operations.
    /// </summary>
    /// <typeparam name="T">The type of the parameter passed to the command.</typeparam>
    public class AsyncCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The function to execute when the command is invoked.</param>
        /// <param name="canExecute">A function that determines whether the command can execute in its current state.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="execute"/> is null.</exception>
        public AsyncCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command can execute.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute((T?)parameter));
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute((T?)parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExecuteAsync(T? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Provides an implementation of <see cref="ICommand"/> that can execute asynchronous operations.
    /// </summary>
    public class AsyncCommand : AsyncCommand<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand"/> class.
        /// </summary>
        /// <param name="execute">The function to execute when the command is invoked.</param>
        /// <param name="canExecute">A function that determines whether the command can execute in its current state.</param>
        public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
            : base(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ExecuteAsync()
        {
            return ExecuteAsync(null);
        }
    }
}
