// ViewModels/LoginViewModel.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Services;
using KeyCard.Desktop.Services.Mock;

namespace KeyCard.Desktop.ViewModels
{
    public sealed class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _auth;
        private readonly INavigationService _nav;
        private readonly IAppEnvironment _env;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _error = string.Empty;
        private bool _isBusy;
        private bool _rememberMe;
        private bool _showUsernameError;
        private bool _showPasswordError;

        public LoginViewModel(IAuthService auth, INavigationService nav, IAppEnvironment env)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _env = env ?? throw new ArgumentNullException(nameof(env));

            LoginCommand = new AsyncRelayCommand(LoginAsync, () => CanLogin);
            ContinueMockCommand = new RelayCommand(ContinueMock, () => IsMockMode);
            RegisterCommand = new RelayCommand(Register);
            ForgotPasswordCommand = new RelayCommand(ForgotPassword);
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ShowUsernameError = false;
                    Reevaluate();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ShowPasswordError = false;
                    Reevaluate();
                }
            }
        }

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

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public bool ShowUsernameError
        {
            get => _showUsernameError;
            private set => SetProperty(ref _showUsernameError, value);
        }

        public bool ShowPasswordError
        {
            get => _showPasswordError;
            private set => SetProperty(ref _showPasswordError, value);
        }

        // Single source of truth for mock flag
        public bool IsMockMode => _env.IsMock || _auth is AuthService;

        // Environment label for badge
        public string EnvironmentLabel => IsMockMode ? "MOCK ENVIRONMENT" : "LIVE PRODUCTION";

        // In Mock, allow Sign in / Enter with empty fields
        public bool CanLogin =>
            !IsBusy && (IsMockMode || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));

        public ICommand LoginCommand { get; }
        public ICommand ContinueMockCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ForgotPasswordCommand { get; }

        private void Reevaluate()
        {
            // Try to raise CanExecuteChanged on commands
            TryRaiseCanExecuteChanged(LoginCommand);
            TryRaiseCanExecuteChanged(ContinueMockCommand);
            OnPropertyChanged(nameof(CanLogin));
        }

        private static void TryRaiseCanExecuteChanged(ICommand? command)
        {
            if (command is null) return;

            // Use reflection to call RaiseCanExecuteChanged if it exists
            var method = command.GetType().GetMethod("RaiseCanExecuteChanged");
            method?.Invoke(command, null);
        }

        public async Task LoginAsync(CancellationToken ct = default)
        {
            if (!CanLogin) return;

            // Validate inputs (except in mock mode)
            if (!IsMockMode)
            {
                var hasErrors = false;

                if (string.IsNullOrWhiteSpace(Username))
                {
                    ShowUsernameError = true;
                    hasErrors = true;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    ShowPasswordError = true;
                    hasErrors = true;
                }

                if (hasErrors)
                {
                    Error = "Please fill in all required fields.";
                    return;
                }
            }

            Error = string.Empty;
            IsBusy = true;

            try
            {
                if (IsMockMode)
                {
                    // Set authenticated state in the auth service before navigating
                    await _auth.LoginMockAsync(ct);
                    _nav.NavigateTo<DashboardViewModel>();
                    return;
                }

                var ok = await _auth.LoginAsync(Username.Trim(), Password, ct);
                if (ok)
                {
                    // TODO: If RememberMe is true, persist credentials securely
                    _nav.NavigateTo<DashboardViewModel>();
                }
                else
                {
                    Error = "Invalid username or password. Please try again.";
                    ShowUsernameError = true;
                    ShowPasswordError = true;
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
            // Mark as authenticated for mock users
            _ = _auth.LoginMockAsync(CancellationToken.None);
            _nav.NavigateTo<DashboardViewModel>();
        }

        private void Register()
        {
            // Navigate to registration view
            _nav.NavigateTo<RegistrationViewModel>();
        }

        private void ForgotPassword()
        {
            // Navigate to password recovery view
            _nav.NavigateTo<ForgotPasswordViewModel>();
        }
    }
}
