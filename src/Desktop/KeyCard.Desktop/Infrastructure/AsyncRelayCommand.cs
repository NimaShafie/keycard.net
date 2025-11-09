// Infrastructure/AsyncRelayCommand.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KeyCard.Desktop.Infrastructure
{
    /// <summary>
    /// Async ICommand helper that supports BOTH parameterless and parameterized async delegates.
    /// Works with:
    ///   new AsyncRelayCommand(async ct => await DoThingAsync(ct), () => CanDoThing)
    ///   new AsyncRelayCommand(async (p, ct) => await DoThingAsync(p, ct), p => CanDoThing(p))
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        // Parameterless delegates
        private readonly Func<CancellationToken, Task>? _executeAsync;
        private readonly Func<bool>? _canExecute;

        // Parameterized delegates
        private readonly Func<object?, CancellationToken, Task>? _executeAsyncParam;
        private readonly Func<object?, bool>? _canExecuteParam;

        private bool _isExecuting;

        // Parameterless ctor
        public AsyncRelayCommand(Func<CancellationToken, Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        // Parameterized ctor
        public AsyncRelayCommand(Func<object?, CancellationToken, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsyncParam = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecuteParam = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_isExecuting) return false;

            if (_canExecuteParam is not null) return _canExecuteParam(parameter);
            return _canExecute?.Invoke() ?? true;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                if (_executeAsyncParam is not null)
                {
                    await _executeAsyncParam(parameter, CancellationToken.None);
                }
                else if (_executeAsync is not null)
                {
                    await _executeAsync(CancellationToken.None);
                }
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        /// <summary>Manually notify the UI to requery CanExecute.</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
