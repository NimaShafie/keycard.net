// ViewModels/RegistrationViewModel.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;

using KeyCard.Desktop.Infrastructure;
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

            RegisterCommand = new DelegateCommand(async () => await RegisterAsync(), () => CanRegister);
            BackToLoginCommand = new DelegateCommand(() => BackToLogin());
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
            (IsMockMode || !string.IsNullOrWhiteSpace(Email)) && // Email optional in Mock mode
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword) &&
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName);

        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }

        private void Reevaluate()
        {
            OnPropertyChanged(nameof(CanRegister));
            (RegisterCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private async Task RegisterAsync(CancellationToken ct = default)
        {
            if (!CanRegister) return;

            Error = string.Empty;

            // In Mock mode, skip email validation
            if (!IsMockMode)
            {
                // Validate email format for Live mode
                if (!IsValidEmail(Email))
                {
                    Error = "Please enter a valid email address.";
                    return;
                }
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
                if (IsMockMode)
                {
                    // Mock mode: Simple local registration (no email required)
                    await Task.Delay(500, ct); // Simulate network delay

                    // TODO: Store in mock data store if needed
                    // For now, just navigate back to login
                    Error = string.Empty;

                    // Show success before navigating
                    await Task.Delay(300, ct);
                    _nav.NavigateTo<LoginViewModel>();
                }
                else
                {
                    // Live mode: Call backend API
                    // TODO: Replace with actual API call
                    // Example:
                    // var request = new StaffRegistrationRequest
                    // {
                    //     Username = Username.Trim(),
                    //     Email = Email.Trim(),
                    //     Password = Password,
                    //     FirstName = FirstName.Trim(),
                    //     LastName = LastName.Trim(),
                    //     EmployeeId = string.IsNullOrWhiteSpace(EmployeeId) ? null : EmployeeId.Trim()
                    // };
                    // 
                    // var result = await _apiService.RegisterStaffAsync(request, ct);

                    // TEMPORARY: Simulate API call until you provide the actual service
                    await Task.Delay(1500, ct);

                    // Simulate successful registration
                    var success = true; // Replace with: result.IsSuccess

                    if (success)
                    {
                        Error = string.Empty;
                        // Show success message before navigating
                        await Task.Delay(500, ct);
                        _nav.NavigateTo<LoginViewModel>();
                    }
                    else
                    {
                        // Handle API error
                        Error = "Registration failed. Please try again or contact support.";
                        // Replace with actual error: result.ErrorMessage
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Error = "Registration cancelled.";
            }
            catch (Exception ex)
            {
                // Network or API error
                Error = IsMockMode
                    ? $"Registration failed: {ex.Message}"
                    : "Unable to connect to the server. Please check your connection and try again.";

                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex}");
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
