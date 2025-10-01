// ViewModels/LoginViewModel.cs
using System;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;

namespace KeyCard.Desktop.ViewModels
{
    public sealed class LoginViewModel : ViewModelBase
    {
        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    OnPropertyChanged(nameof(CanLogin));
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    OnPropertyChanged(nameof(CanLogin));
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanLogin));
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // Exposed for XAML: IsEnabled="{Binding CanLogin}"
        public bool CanLogin =>
            !IsBusy &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                    OnPropertyChanged(nameof(Error)); // keep alias in sync
            }
        }

        // Alias to match XAML binding {Binding Error}
        public string Error
        {
            get => ErrorMessage;
            set => ErrorMessage = value;
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(
                execute: () => _ = LoginAsync(),
                canExecute: () => CanLogin
            );
        }

        private async Task LoginAsync()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                // TODO: replace with real auth call
                await Task.Delay(300);

                if (!Username.Equals("admin", StringComparison.OrdinalIgnoreCase) || Password != "password")
                {
                    ErrorMessage = "Invalid username or password.";
                    return;
                }

                // Success: navigate or update app state here
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
