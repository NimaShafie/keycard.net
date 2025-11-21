// ViewModels/RegistrationViewModel.cs
using System;
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
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _employeeId = string.Empty;
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

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
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

        public string EmployeeId
        {
            get => _employeeId;
            set => SetProperty(ref _employeeId, value);
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
            !string.IsNullOrWhiteSpace(Email) &&
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

            // Validate inputs
            if (Password != ConfirmPassword)
            {
                Error = "Passwords do not match.";
                return;
            }

            if (Password.Length < 8)
            {
                Error = "Password must be at least 8 characters long.";
                return;
            }

            if (!Email.Contains("@"))
            {
                Error = "Please enter a valid email address.";
                return;
            }

            Error = string.Empty;
            IsBusy = true;

            try
            {
                // ✅ CRITICAL FIX: Ensure LastName is never null or empty
                // Backend expects LastName to build FullName
                var lastName = string.IsNullOrWhiteSpace(LastName) ? "User" : LastName.Trim();
                var firstName = FirstName.Trim();
                var username = Username.Trim();
                var email = Email.Trim();
                var employeeId = string.IsNullOrWhiteSpace(EmployeeId) ? null : EmployeeId.Trim();

                var (success, errorMessage) = await _authService.RegisterStaffAsync(
                    username: username,
                    email: email,
                    password: Password,
                    firstName: firstName,
                    lastName: lastName,  // ✅ Never null
                    employeeId: employeeId,
                    ct: ct
                );

                if (success)
                {
                    // Registration successful - navigate back to login
                    _navigationService.NavigateTo<LoginViewModel>();
                }
                else
                {
                    Error = errorMessage ?? "Registration failed. Please try again.";
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
    }
}
