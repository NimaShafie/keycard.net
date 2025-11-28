// ViewModels/RegistrationViewModel.cs
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    public sealed class RegistrationViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IAppEnvironment _env;

        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _error = string.Empty;
        private string _successMessage = string.Empty;
        private bool _isBusy;

        public RegistrationViewModel(
            IAuthService authService,
            INavigationService navigationService,
            IAppEnvironment env)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _env = env ?? throw new ArgumentNullException(nameof(env));

            RegisterCommand = new DelegateCommand(async () => await RegisterAsync(), () => CanRegister);
            BackToLoginCommand = new DelegateCommand(() => BackToLogin());
        }

        public string FirstName
        {
            get => _firstName;
            set
            {
                if (SetProperty(ref _firstName, value))
                    RaiseCanExecuteChanged();
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value))
                    RaiseCanExecuteChanged();
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                    RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                    RaiseCanExecuteChanged();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                    RaiseCanExecuteChanged();
            }
        }

        public string Error
        {
            get => _error;
            private set => SetProperty(ref _error, value);
        }

        public string SuccessMessage
        {
            get => _successMessage;
            private set => SetProperty(ref _successMessage, value);
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

        public bool IsMockMode => _env.IsMock;

        public string EnvironmentLabel => IsMockMode ? "MOCK ENVIRONMENT" : "LIVE PRODUCTION";

        public bool CanRegister =>
            !IsBusy &&
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName) &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword) &&
            Password.Length >= 8 &&
            Password == ConfirmPassword;

        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }

        private void RaiseCanExecuteChanged()
        {
            OnPropertyChanged(nameof(CanRegister));
            (RegisterCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private async Task RegisterAsync(CancellationToken ct = default)
        {
            if (!CanRegister) return;

            // Clear previous messages
            Error = string.Empty;
            SuccessMessage = string.Empty;

            System.Diagnostics.Debug.WriteLine("=== REGISTRATION STARTED ===");
            System.Diagnostics.Debug.WriteLine($"Username: {Username}");
            System.Diagnostics.Debug.WriteLine($"FirstName: {FirstName}");
            System.Diagnostics.Debug.WriteLine($"LastName: {LastName}");
            System.Diagnostics.Debug.WriteLine($"Mock Mode: {IsMockMode}");

            // Validate passwords match
            if (Password != ConfirmPassword)
            {
                Error = "Passwords do not match.";
                System.Diagnostics.Debug.WriteLine("ERROR: Passwords do not match");
                return;
            }

            // Validate password length
            if (Password.Length < 8)
            {
                Error = "Password must be at least 8 characters long.";
                System.Diagnostics.Debug.WriteLine("ERROR: Password too short");
                return;
            }

            // Validate password complexity: 1 uppercase, 1 number, 1 special character
            if (!HasPasswordComplexity(Password))
            {
                Error = "Password must contain at least one uppercase letter, one number, and one special character.";
                System.Diagnostics.Debug.WriteLine("ERROR: Password complexity requirements not met");
                return;
            }

            // Validate username is not empty
            if (string.IsNullOrWhiteSpace(Username))
            {
                Error = "Username is required.";
                System.Diagnostics.Debug.WriteLine("ERROR: Username is empty");
                return;
            }

            IsBusy = true;

            try
            {
                // Prepare cleaned inputs
                var lastName = string.IsNullOrWhiteSpace(LastName) ? "User" : LastName.Trim();
                var firstName = FirstName.Trim();
                var username = Username.Trim();

                // Generate a dummy email since backend expects it
                var email = $"{username}@keycard.local";

                System.Diagnostics.Debug.WriteLine($"Calling RegisterStaffAsync with:");
                System.Diagnostics.Debug.WriteLine($"  Username: {username}");
                System.Diagnostics.Debug.WriteLine($"  Email: {email}");
                System.Diagnostics.Debug.WriteLine($"  FirstName: {firstName}");
                System.Diagnostics.Debug.WriteLine($"  LastName: {lastName}");
                System.Diagnostics.Debug.WriteLine($"  Password length: {Password.Length}");

                var (success, errorMessage) = await _authService.RegisterStaffAsync(
                    username: username,
                    email: email,
                    password: Password,
                    firstName: firstName,
                    lastName: lastName,
                    employeeId: null,
                    ct: ct
                );

                System.Diagnostics.Debug.WriteLine($"RegisterStaffAsync returned:");
                System.Diagnostics.Debug.WriteLine($"  Success: {success}");
                System.Diagnostics.Debug.WriteLine($"  ErrorMessage: {errorMessage ?? "null"}");

                // ✅ CRITICAL FIX: Only show success if actually successful
                if (success && string.IsNullOrEmpty(errorMessage))
                {
                    // Registration successful
                    SuccessMessage = $"Account created successfully! Welcome, {firstName}!";
                    System.Diagnostics.Debug.WriteLine("=== REGISTRATION SUCCESSFUL ===");

                    // Wait 2 seconds to show success message, then navigate to login
                    await Task.Delay(2000, ct);
                    _navigationService.NavigateTo<LoginViewModel>();
                }
                else
                {
                    // Registration failed
                    System.Diagnostics.Debug.WriteLine("=== REGISTRATION FAILED ===");

                    // Check if error is about duplicate username
                    if (errorMessage?.Contains("username", StringComparison.OrdinalIgnoreCase) == true ||
                        errorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true ||
                        errorMessage?.Contains("taken", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        Error = "Username already exists. Please choose a different username.";
                        System.Diagnostics.Debug.WriteLine("ERROR: Username already exists");
                    }
                    else
                    {
                        // Show the actual error message from backend
                        Error = errorMessage ?? "Registration failed. Please check your information and try again.";
                        System.Diagnostics.Debug.WriteLine($"ERROR: {errorMessage ?? "Unknown error"}");
                    }
                }
            }
            catch (System.Net.Http.HttpRequestException httpEx)
            {
                Error = "Cannot connect to server. Please ensure the backend is running.";
                System.Diagnostics.Debug.WriteLine($"=== HTTP ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Message: {httpEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {httpEx.StackTrace}");
            }
            catch (Exception ex)
            {
                Error = $"Registration failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"=== EXCEPTION ===");
                System.Diagnostics.Debug.WriteLine($"Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            }
            finally
            {
                IsBusy = false;
                System.Diagnostics.Debug.WriteLine("=== REGISTRATION ENDED ===");
            }
        }

        private void BackToLogin()
        {
            _navigationService.NavigateTo<LoginViewModel>();
        }

        /// <summary>
        /// Validates password complexity:
        /// - At least one uppercase letter
        /// - At least one number
        /// - At least one special character
        /// </summary>
        private static bool HasPasswordComplexity(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            System.Diagnostics.Debug.WriteLine($"Password complexity check:");
            System.Diagnostics.Debug.WriteLine($"  Has uppercase: {hasUpper}");
            System.Diagnostics.Debug.WriteLine($"  Has digit: {hasDigit}");
            System.Diagnostics.Debug.WriteLine($"  Has special: {hasSpecial}");

            return hasUpper && hasDigit && hasSpecial;
        }
    }
}
