// Infrastructure/UnifiedRelayCommand.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KeyCard.Desktop.Infrastructure
{
    /// <summary>
    /// Unified ICommand implementation supporting sync/async execution with optional cancellation.
    /// Use this instead of multiple RelayCommand implementations.
    /// </summary>
    public sealed class UnifiedRelayCommand : ICommand, IDisposable
    {
        private readonly Func<object?, Task>? _executeAsync;
        private readonly Action<object?>? _executeSync;
        private readonly Func<object?, bool>? _canExecute;
        private CancellationTokenSource? _cts;
        private bool _isExecuting;
        private bool _disposed;

        // Parameterless constructors
        public UnifiedRelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute is null ? null : _ => canExecute())
        {
        }

        public UnifiedRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
            : this(_ => executeAsync(), canExecute is null ? null : _ => canExecute())
        {
        }

        public UnifiedRelayCommand(Func<CancellationToken, Task> executeAsync, Func<bool>? canExecute = null)
            : this(async param =>
            {
                var cts = (param as UnifiedRelayCommand)?._cts;
                await executeAsync(cts?.Token ?? CancellationToken.None);
            }, canExecute is null ? null : _ => canExecute())
        {
        }

        // Parameterized constructors
        public UnifiedRelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeSync = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public UnifiedRelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_disposed) return false;
            if (_isExecuting) return false; // block reentrancy while running
            return _canExecute?.Invoke(parameter) ?? true; // default true when no predicate
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            try
            {
                if (_executeSync is not null)
                {
                    _executeSync(parameter);
                    return;
                }

                if (_executeAsync is not null)
                {
                    _isExecuting = true;
                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();
                    RaiseCanExecuteChanged();

                    await _executeAsync(parameter);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }
            catch (Exception)
            {
                // Let caller handle/log as needed
                throw;
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        /// <summary>Cancel the async operation if currently executing.</summary>
        public void Cancel()
        {
            if (_cts is not null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _cts?.Dispose();
            _cts = null;
            _disposed = true;
            RaiseCanExecuteChanged();
        }
    }
}
