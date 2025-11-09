// ViewModels/RegistrationViewModel.cs
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    public sealed class RegistrationViewModel : ViewModelBase
    {
        private readonly IAuthService _auth;
        private readonly INavigationService _nav;
        private readonly IAppEnvironment _env;

        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _employeeId = string.Empty;
        private string _error = string.Empty;
        private bool _isBusy;

        public RegistrationViewModel(IAuthService auth, INavigationService nav, IAppEnvironment env)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _env = env ?? throw new ArgumentNullException(nameof(env));

            RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => CanRegister);
            BackToLoginCommand = new RelayCommand(BackToLogin);
        }

        public string Username
        {
            get => _username;
            set { if (SetProperty(ref _username, value)) Reevaluate(); }
        }

        public string Email
        {
            get => _email;
            set { if (SetProperty(ref _email, value)) Reevaluate(); }
        }

        public string Password
        {
            get => _password;
            set { if (SetProperty(ref _password, value)) Reevaluate(); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { if (SetProperty(ref _confirmPassword, value)) Reevaluate(); }
        }

        public string FirstName
        {
            get => _firstName;
            set { if (SetProperty(ref _firstName, value)) Reevaluate(); }
        }

        public string LastName
        {
            get => _lastName;
            set { if (SetProperty(ref _lastName, value)) Reevaluate(); }
        }

        public string EmployeeId
        {
            get => _employeeId;
            set { if (SetProperty(ref _employeeId, value)) Reevaluate(); }
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

        public bool IsMockMode => _env.IsMock;

        public bool CanRegister => !IsBusy &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword) &&
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName);

        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }

        private void Reevaluate()
        {
            TryRaiseCanExecuteChanged(RegisterCommand);
            OnPropertyChanged(nameof(CanRegister));
        }

        private static void TryRaiseCanExecuteChanged(ICommand? command)
        {
            if (command is null) return;

            // Use reflection to call RaiseCanExecuteChanged if it exists
            var method = command.GetType().GetMethod("RaiseCanExecuteChanged");
            method?.Invoke(command, null);
        }

        private async Task RegisterAsync(CancellationToken ct = default)
        {
            if (!CanRegister) return;

            Error = string.Empty;

            // Validate email format
            if (!IsValidEmail(Email))
            {
                Error = "Please enter a valid email address.";
                return;
            }

            // Validate password match
            if (Password != ConfirmPassword)
            {
                Error = "Passwords do not match.";
                return;
            }

            // Validate password strength
            if (Password.Length < 8)
            {
                Error = "Password must be at least 8 characters long.";
                return;
            }

            IsBusy = true;

            try
            {
                // TODO: Call registration API endpoint
                // For now, simulate registration
                await Task.Delay(1000, ct);

                if (IsMockMode)
                {
                    // In mock mode, registration always succeeds
                    Error = string.Empty;
                    _nav.NavigateTo<LoginViewModel>();
                }
                else
                {
                    // In live mode, you would call your API here
                    // var result = await _api.RegisterStaffAsync(new RegistrationRequest { ... });

                    // For now, simulate success
                    Error = string.Empty;
                    _nav.NavigateTo<LoginViewModel>();
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
            _nav.NavigateTo<LoginViewModel>();
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}
