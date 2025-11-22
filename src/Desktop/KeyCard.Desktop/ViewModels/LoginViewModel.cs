// ViewModels/LoginViewModel.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Services;

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
        private string _statusMessage = string.Empty;
        private string _backendStatus = string.Empty;
        private bool _isBusy;
        private bool _rememberMe;
        private bool _showUsernameError;
        private bool _showPasswordError;
        private bool _isCheckingBackend;
        private bool _backendCheckFailed;
        private bool _backendReady;
        private string _loadingDots = "";

        public LoginViewModel(IAuthService auth, INavigationService nav, IAppEnvironment env)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _env = env ?? throw new ArgumentNullException(nameof(env));

            LoginCommand = new DelegateCommand(async () => await LoginAsync(), () => CanLogin);
            ContinueMockCommand = new DelegateCommand(() => ContinueMock(), () => IsMockMode);
            RegisterCommand = new DelegateCommand(() => Register());
            ForgotPasswordCommand = new DelegateCommand(() => ForgotPassword());

            // CRITICAL: Initialize button state BEFORE the view is shown
            if (!IsMockMode)
            {
                System.Diagnostics.Debug.WriteLine("=== CONSTRUCTOR: Setting initial loading state ===");
                _isCheckingBackend = true;
                _signInButtonText = "Loading Backend";
                System.Diagnostics.Debug.WriteLine($"=== CONSTRUCTOR: IsCheckingBackend={_isCheckingBackend}, SignInButtonText={_signInButtonText} ===");
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ShowUsernameError = false;
                    RaiseCanExecuteChanged();
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
                    RaiseCanExecuteChanged();
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
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    RaiseCanExecuteChanged();
            }
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

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string BackendStatus
        {
            get => _backendStatus;
            private set => SetProperty(ref _backendStatus, value);
        }

        public string LoadingDots
        {
            get => _loadingDots;
            private set
            {
                if (SetProperty(ref _loadingDots, value))
                {
                    UpdateSignInButtonText();
                }
            }
        }

        private string _signInButtonText = "Sign In";
        public string SignInButtonText
        {
            get => _signInButtonText;
            private set => SetProperty(ref _signInButtonText, value);
        }

        private void UpdateSignInButtonText()
        {
            SignInButtonText = IsCheckingBackend ? $"Loading Backend{LoadingDots}" : "Sign In";
        }

        public bool IsCheckingBackend
        {
            get => _isCheckingBackend;
            private set
            {
                if (SetProperty(ref _isCheckingBackend, value))
                {
                    RaiseCanExecuteChanged();
                    UpdateSignInButtonText();
                }
            }
        }

        public bool BackendCheckFailed
        {
            get => _backendCheckFailed;
            private set
            {
                if (SetProperty(ref _backendCheckFailed, value))
                    RaiseCanExecuteChanged();
            }
        }

        public bool BackendReady
        {
            get => _backendReady;
            private set
            {
                if (SetProperty(ref _backendReady, value))
                    RaiseCanExecuteChanged();
            }
        }

        public bool IsMockMode => _env.IsMock;
        public string EnvironmentLabel => IsMockMode ? "MOCK ENVIRONMENT" : "LIVE PRODUCTION";

        public bool CanLogin => !IsBusy && !IsCheckingBackend && (BackendReady || IsMockMode) &&
            (IsMockMode || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));

        public bool CanRegister => !IsCheckingBackend && (BackendReady || IsMockMode);

        public ICommand LoginCommand { get; }
        public ICommand ContinueMockCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ForgotPasswordCommand { get; }

        private void RaiseCanExecuteChanged()
        {
            OnPropertyChanged(nameof(CanLogin));
            OnPropertyChanged(nameof(CanRegister));
            (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ContinueMockCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (RegisterCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        public async Task LoginAsync(CancellationToken ct = default)
        {
            if (!CanLogin) return;

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
                    await _auth.LoginMockAsync(ct);
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
            _ = _auth.LoginMockAsync(CancellationToken.None);
            _nav.NavigateTo<DashboardViewModel>();
        }

        private void Register()
        {
            _nav.NavigateTo<RegistrationViewModel>();
        }

        private void ForgotPassword()
        {
            _nav.NavigateTo<ForgotPasswordViewModel>();
        }

        public async Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== InitializeAsync STARTED ===");
            System.Diagnostics.Debug.WriteLine("=== WATCH THE LOGIN SCREEN - 15 SECOND LOADING ANIMATION COMING! ===");

            try
            {
                System.Diagnostics.Debug.WriteLine($"IsMockMode: {IsMockMode}");

                if (IsMockMode)
                {
                    System.Diagnostics.Debug.WriteLine("Mock mode - setting BackendReady = true");
                    BackendReady = true;
                    System.Diagnostics.Debug.WriteLine("=== InitializeAsync COMPLETED (Mock) ===");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Starting CheckBackendAsync...");
                await CheckBackendAsync();
                System.Diagnostics.Debug.WriteLine("=== InitializeAsync COMPLETED ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! InitializeAsync EXCEPTION: {ex}");
                System.Diagnostics.Debug.WriteLine($"!!! Stack Trace: {ex.StackTrace}");

                BackendCheckFailed = true;
                IsCheckingBackend = false;
                BackendReady = false;
                StatusMessage = $"Initialization error: {ex.Message}";

                throw; // Re-throw to see in debugger
            }
        }

        private async Task CheckBackendAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== CheckBackendAsync STARTED ===");

            StatusMessage = "Connecting to backend...";
            BackendStatus = "Connecting to backend...";
            IsCheckingBackend = true;
            BackendReady = false;
            BackendCheckFailed = false;

            System.Diagnostics.Debug.WriteLine("=== IsCheckingBackend SET TO TRUE ===");
            System.Diagnostics.Debug.WriteLine("=== BUTTON SHOULD NOW BE RED WITH 'Loading Backend...' ===");

            // Start animated dots timer IMMEDIATELY
            var dotTimer = new System.Timers.Timer(500); // Update every 500ms (0.5 seconds)
            var dotCount = 0;
            dotTimer.Elapsed += (s, e) =>
            {
                dotCount = (dotCount + 1) % 4; // Cycle 0, 1, 2, 3
                var dots = new string('.', dotCount);

                // CRITICAL: Update on UI thread using Dispatcher
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    LoadingDots = dots;
                    System.Diagnostics.Debug.WriteLine($"Dots updated: '{LoadingDots}' (count: {dotCount})");
                });
            };
            dotTimer.Start();
            System.Diagnostics.Debug.WriteLine("=== TIMER STARTED ===");

            try
            {
                // TEMPORARY: Add 15 second delay to see the loading animation
                System.Diagnostics.Debug.WriteLine("=== STARTING 15 SECOND DELAY - WATCH THE BUTTON! ===");
                await Task.Delay(15000);
                System.Diagnostics.Debug.WriteLine("=== 15 SECOND DELAY COMPLETE ===");

                // Validate API URL
                if (string.IsNullOrWhiteSpace(_env.ApiBaseUrl))
                {
                    StatusMessage = "Backend URL not configured";
                    BackendStatus = "Backend URL not configured";
                    BackendCheckFailed = true;
                    IsCheckingBackend = false;
                    return;
                }

                var maxAttempts = 30; // 15 seconds
                var attempt = 0;

                while (attempt < maxAttempts)
                {
                    attempt++;
                    StatusMessage = $"Connecting to backend... ({attempt}/{maxAttempts})";
                    BackendStatus = $"Connecting... ({attempt}/{maxAttempts})";

                    var isAlive = await PingBackendHealthAsync();
                    if (isAlive)
                    {
                        StatusMessage = "Backend connected";
                        BackendStatus = "Connected!";
                        BackendReady = true;
                        IsCheckingBackend = false;
                        System.Diagnostics.Debug.WriteLine("=== BACKEND CONNECTED - BUTTON SHOULD TURN PURPLE ===");
                        await Task.Delay(1500);
                        StatusMessage = string.Empty;
                        BackendStatus = string.Empty;
                        return;
                    }

                    await Task.Delay(500);
                }

                StatusMessage = "Connection to backend could not be established";
                BackendStatus = "Backend unavailable";
                BackendCheckFailed = true;
                IsCheckingBackend = false;
                BackendReady = false;
            }
            finally
            {
                dotTimer.Stop();
                dotTimer.Dispose();
                LoadingDots = "";
                System.Diagnostics.Debug.WriteLine("=== TIMER STOPPED ===");
            }
        }

        private async Task<bool> PingBackendHealthAsync()
        {
            try
            {
                using var handler = new System.Net.Http.HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var client = new System.Net.Http.HttpClient(handler)
                {
                    BaseAddress = new Uri(_env.ApiBaseUrl),
                    Timeout = TimeSpan.FromSeconds(2)
                };

                var endpoints = new[] { "/api/v1/Health", "/health", "/api/health" };

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var response = await client.GetAsync(endpoint);
                        if (response.IsSuccessStatusCode)
                            return true;
                    }
                    catch
                    {
                        // Try next endpoint
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
