// ViewModels/ForgotPasswordViewModel.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    public sealed class ForgotPasswordViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private readonly IAppEnvironment _env;

        private string _email = string.Empty;
        private string _message = string.Empty;
        private bool _isSuccess;
        private bool _isBusy;

        public ForgotPasswordViewModel(INavigationService nav, IAppEnvironment env)
        {
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _env = env ?? throw new ArgumentNullException(nameof(env));

            SendResetLinkCommand = new DelegateCommand(async () => await SendResetLinkAsync(), () => CanSendResetLink);
            BackToLoginCommand = new DelegateCommand(() => BackToLogin());
        }

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    Message = string.Empty;
                    IsSuccess = false;
                    Reevaluate();
                }
            }
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            private set => SetProperty(ref _isSuccess, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { if (SetProperty(ref _isBusy, value)) Reevaluate(); }
        }

        public bool IsMockMode => _env.IsMock;

        public bool CanSendResetLink => !IsBusy && !string.IsNullOrWhiteSpace(Email);

        public ICommand SendResetLinkCommand { get; }
        public ICommand BackToLoginCommand { get; }

        private void Reevaluate()
        {
            OnPropertyChanged(nameof(CanSendResetLink));
            (SendResetLinkCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private async Task SendResetLinkAsync(CancellationToken ct = default)
        {
            if (!CanSendResetLink) return;

            Message = string.Empty;
            IsSuccess = false;

            // Validate email format
            if (!IsValidEmail(Email))
            {
                Message = "Please enter a valid email address.";
                IsSuccess = false;
                return;
            }

            IsBusy = true;

            try
            {
                // TODO: Call password reset API endpoint
                // For now, simulate sending reset link
                await Task.Delay(1500, ct);

                if (IsMockMode)
                {
                    Message = "Password reset link sent! Check your email inbox.";
                    IsSuccess = true;
                }
                else
                {
                    // In live mode, you would call your API here
                    // var result = await _api.SendPasswordResetAsync(Email);

                    // For now, simulate success
                    Message = "If an account exists with this email, you will receive a password reset link shortly.";
                    IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Message = $"Failed to send reset link: {ex.Message}";
                IsSuccess = false;
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
