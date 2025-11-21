// ViewModels/StartupViewModel.cs
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    /// <summary>
    /// Splash screen that waits for backend to be ready before showing login.
    /// </summary>
    public sealed class StartupViewModel : ViewModelBase
    {
        private readonly IAppEnvironment _env;
        private readonly INavigationService _nav;
        private readonly IHttpClientFactory _httpClientFactory;

        private string _statusMessage = "Initializing...";
        private bool _isChecking = true;
        private bool _hasError;
        private string _errorMessage = string.Empty;

        public StartupViewModel(
            IAppEnvironment env,
            INavigationService nav,
            IHttpClientFactory httpClientFactory)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsChecking
        {
            get => _isChecking;
            private set => SetProperty(ref _isChecking, value);
        }

        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool IsMockMode => _env.IsMock;

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            if (_env.IsMock)
            {
                StatusMessage = "Mock mode - Ready";
                await Task.Delay(500, ct); // Brief delay for visual feedback
                _nav.NavigateTo<LoginViewModel>();
                return;
            }

            StatusMessage = "Checking backend connection...";

            var maxAttempts = 30; // 30 attempts = ~15 seconds
            var attempt = 0;

            while (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                attempt++;
                StatusMessage = $"Waiting for backend... (attempt {attempt}/{maxAttempts})";

                var (success, message) = await CheckBackendHealthAsync(ct);

                if (success)
                {
                    StatusMessage = "Backend connected - Ready!";
                    await Task.Delay(500, ct); // Brief success message display
                    _nav.NavigateTo<LoginViewModel>();
                    return;
                }

                if (attempt >= maxAttempts)
                {
                    HasError = true;
                    IsChecking = false;
                    ErrorMessage = $"Backend not responding after {maxAttempts} attempts.\n\n" +
                                 $"Please ensure the backend is running at:\n{_env.ApiBaseUrl}\n\n" +
                                 $"Last error: {message}";
                    StatusMessage = "Connection failed";

                    // Still navigate to login so user can try anyway
                    await Task.Delay(3000, ct);
                    _nav.NavigateTo<LoginViewModel>();
                    return;
                }

                await Task.Delay(500, ct); // Wait 500ms between attempts
            }
        }

        private async Task<(bool success, string message)> CheckBackendHealthAsync(CancellationToken ct)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("Api");

                // Try multiple health check endpoints
                var endpoints = new[] { "/api/v1/Health", "/health", "/api/health" };

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        using var response = await client.GetAsync(endpoint, ct);
                        if (response.IsSuccessStatusCode)
                        {
                            return (true, "Backend is healthy");
                        }
                    }
                    catch
                    {
                        // Try next endpoint
                    }
                }

                return (false, "No health endpoint responded");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Connection refused: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "Request timed out");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
