// ViewModels/RegistrationViewModel.cs
using System;
using System.Linq;
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

            // Validate passwords match
            if (Password != ConfirmPassword)
            {
                Error = "Passwords do not match.";
                return;
            }

            // Validate password length
            if (Password.Length < 8)
            {
                Error = "Password must be at least 8 characters long.";
                return;
            }

            // Validate password complexity: 1 uppercase, 1 number, 1 special character
            if (!HasPasswordComplexity(Password))
            {
                Error = "Password must contain at least one uppercase letter, one number, and one special character.";
                return;
            }

            // Validate username is not empty
            if (string.IsNullOrWhiteSpace(Username))
            {
                Error = "Username is required.";
                return;
            }

            Error = string.Empty;
            IsBusy = true;

            try
            {
                // Prepare cleaned inputs
                var lastName = string.IsNullOrWhiteSpace(LastName) ? "User" : LastName.Trim();
                var firstName = FirstName.Trim();
                var username = Username.Trim();

                // Generate a dummy email since backend expects it
                // Using username@keycard.local for school project
                var email = $"{username}@keycard.local";

                var (success, errorMessage) = await _authService.RegisterStaffAsync(
                    username: username,
                    email: email,
                    password: Password,
                    firstName: firstName,
                    lastName: lastName,
                    employeeId: null,  // No longer collecting employee ID
                    ct: ct
                );

                if (success)
                {
                    // Registration successful - navigate back to login
                    _navigationService.NavigateTo<LoginViewModel>();
                }
                else
                {
                    // Check if error is about duplicate username
                    if (errorMessage?.Contains("username", StringComparison.OrdinalIgnoreCase) == true ||
                        errorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        Error = "Username already exists. Please choose a different username.";
                    }
                    else
                    {
                        Error = errorMessage ?? "Registration failed. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                Error = $"Registration failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
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

            return hasUpper && hasDigit && hasSpecial;
        }
    }
}
