// Infrastructure/DelegateCommand.cs
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KeyCard.Desktop.Infrastructure
{
    /// <summary>
    /// Simple command implementation that supports both synchronous and asynchronous operations.
    /// </summary>
    public sealed class DelegateCommand : ICommand
    {
        private readonly Func<Task>? _executeAsync;
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Creates a synchronous command.
        /// </summary>
        public DelegateCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Creates an asynchronous command.
        /// </summary>
        public DelegateCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                if (_executeAsync != null)
                    await _executeAsync();
                else
                    _execute?.Invoke();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
