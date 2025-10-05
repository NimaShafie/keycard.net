using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    // Assumes you have a ViewModelBase with SetProperty/OnPropertyChanged
    public sealed class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _auth;
        private readonly INavigationService _nav;
        private readonly bool _isMockMode;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _error = string.Empty;
        private bool _isBusy;

        public LoginViewModel(IAuthService auth, INavigationService nav, IConfiguration config)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));

            _isMockMode =
                string.Equals(config["KeyCard:Mode"], "Mock", StringComparison.OrdinalIgnoreCase) ||
                (bool.TryParse(config["UseMocks"], out var useMocks) && useMocks);

            LoginCommand = new AsyncActionCommand(LoginAsync, () => CanLogin);
            ContinueMockCommand = new ActionCommand(ContinueMock, () => IsMockMode);
        }

        public string Username
        {
            get => _username;
            set { if (SetProperty(ref _username, value)) Reevaluate(); }
        }

        public string Password
        {
            get => _password;
            set { if (SetProperty(ref _password, value)) Reevaluate(); }
        }

        // Same property name you already use in XAML
        public string Error
        {
            get => _error;
            private set => SetProperty(ref _error, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { if (SetProperty(ref _isBusy, value)) Reevaluate(); }
        }

        public bool IsMockMode => _isMockMode;

        // Preserves your binding logic
        public bool CanLogin => !IsBusy && (IsMockMode || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));

        public ICommand LoginCommand { get; }
        public ICommand ContinueMockCommand { get; }

        private void Reevaluate()
        {
            (LoginCommand as AsyncActionCommand)?.RaiseCanExecuteChanged();
            (ContinueMockCommand as ActionCommand)?.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CanLogin));
        }

        public async Task LoginAsync(CancellationToken ct = default)
        {
            if (!CanLogin) return;

            Error = string.Empty;
            IsBusy = true;

            try
            {
                if (IsMockMode)
                {
                    _nav.NavigateTo<DashboardViewModel>();
                    return;
                }

                var ok = await _auth.LoginAsync(Username.Trim(), Password, ct);
                if (ok)
                {
                    _nav.NavigateTo<DashboardViewModel>();
                }
                else
                {
                    Error = "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                Error = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ContinueMock()
        {
            if (!IsMockMode) return;
            _nav.NavigateTo<DashboardViewModel>();
        }
    }

    // ----- Local command helpers with correct nullability -----

    internal sealed class ActionCommand : ICommand
    {
        private readonly Action _exec;
        private readonly Func<bool>? _can;
        public event EventHandler? CanExecuteChanged;

        public ActionCommand(Action exec, Func<bool>? can = null)
        { _exec = exec; _can = can; }

        public bool CanExecute(object? parameter) => _can?.Invoke() ?? true;
        public void Execute(object? parameter) => _exec();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    internal sealed class AsyncActionCommand : ICommand
    {
        private readonly Func<CancellationToken, Task> _execAsync;
        private readonly Func<bool>? _can;
        private bool _running;

        public event EventHandler? CanExecuteChanged;

        public AsyncActionCommand(Func<CancellationToken, Task> execAsync, Func<bool>? can = null)
        {
            _execAsync = execAsync;
            _can = can;
        }

        public bool CanExecute(object? parameter) => !_running && (_can?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(null)) return;
            try
            {
                _running = true; RaiseCanExecuteChanged();
                await _execAsync(CancellationToken.None);
            }
            finally
            {
                _running = false; RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
