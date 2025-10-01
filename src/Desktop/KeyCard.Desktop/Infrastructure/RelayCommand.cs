//Infrastructure/RelayCommand.cs
using System;
using System.Windows.Input;

namespace KeyCard.Desktop.Infrastructure
{
    /// <summary>
    /// ICommand helper that supports BOTH parameterless and parameterized delegates.
    /// Works with:
    ///   new RelayCommand(() => DoThing(), () => CanDoThing)
    ///   new RelayCommand(p => DoThing(p), p => CanDoThing(p))
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        // Parameterless delegates
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        // Parameterized delegates
        private readonly Action<object?>? _executeParam;
        private readonly Func<object?, bool>? _canExecuteParam;

        // Parameterless ctor
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Parameterized ctor
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteParam = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecuteParam is not null) return _canExecuteParam(parameter);
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            if (_executeParam is not null) { _executeParam(parameter); return; }
            _execute?.Invoke();
        }

        public event EventHandler? CanExecuteChanged;

        /// <summary>Manually notify the UI to requery CanExecute.</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
