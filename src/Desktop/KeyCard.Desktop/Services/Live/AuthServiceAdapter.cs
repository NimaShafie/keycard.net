// Services/Live/AuthServiceAdapter.cs - PRODUCTION VERSION (No Diagnostics)
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services.Live
{
    /// <summary>
    /// Live adapter that calls the backend API for authentication.
    /// </summary>
    public sealed class AuthServiceAdapter : IAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAppEnvironment _env;
        private bool _isAuthenticated;
        private string _displayName = string.Empty;

        public event EventHandler? StateChanged;

        public AuthServiceAdapter(IHttpClientFactory httpClientFactory, IAppEnvironment env)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set
            {
                if (_isAuthenticated != value)
                {
                    _isAuthenticated = value;
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string DisplayName
        {
            get => _displayName;
            private set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            try
            {
                var payload = new { Username = username, Password = password };
                var response = await Client.PostAsJsonAsync("api/auth/login", payload, ct);

                if (response.IsSuccessStatusCode)
                {
                    IsAuthenticated = true;
                    DisplayName = username;
                    return true;
                }

                IsAuthenticated = false;
                DisplayName = string.Empty;
                return false;
            }
            catch
            {
                IsAuthenticated = false;
                DisplayName = string.Empty;
                return false;
            }
        }

        public Task<bool> LoginMockAsync(CancellationToken ct = default)
        {
            IsAuthenticated = true;
            DisplayName = "Mock User";
            return Task.FromResult(true);
        }

        public Task<bool> SignInAsync(string username, string password, CancellationToken ct = default)
            => LoginAsync(username, password, ct);

        public Task<bool> SignInMockAsync(CancellationToken ct = default)
            => LoginMockAsync(ct);

        public async Task<(bool success, string? errorMessage)> RegisterStaffAsync(
            string username,
            string email,
            string password,
            string firstName,
            string lastName,
            string? employeeId = null,
            CancellationToken ct = default)
        {
            try
            {
                var safeLastName = string.IsNullOrWhiteSpace(lastName) ? "User" : lastName.Trim();
                var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "Guest" : firstName.Trim();
                var safeUsername = username.Trim();

                var payload = new
                {
                    Username = safeUsername,
                    Email = email.Trim(),
                    FirstName = safeFirstName,
                    LastName = safeLastName,
                    Password = password
                };

                var response = await Client.PostAsJsonAsync("api/auth/staff/register", payload, ct);

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                // Try to parse error message
                string? errorMessage = null;
                try
                {
                    var responseBody = await response.Content.ReadAsStringAsync(ct);
                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        using var doc = JsonDocument.Parse(responseBody);
                        if (doc.RootElement.TryGetProperty("message", out var msgElement))
                        {
                            errorMessage = msgElement.GetString();
                        }
                        else if (doc.RootElement.TryGetProperty("title", out var titleElement))
                        {
                            errorMessage = titleElement.GetString();
                        }
                        else if (doc.RootElement.TryGetProperty("errors", out var errorsElement))
                        {
                            errorMessage = errorsElement.ToString();
                        }
                        else
                        {
                            errorMessage = responseBody;
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors
                }

                errorMessage ??= $"Registration failed with status {(int)response.StatusCode}";
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                return (false, $"Unable to connect to the server: {ex.Message}");
            }
        }

        public void Logout()
        {
            IsAuthenticated = false;
            DisplayName = string.Empty;
        }
    }
}
