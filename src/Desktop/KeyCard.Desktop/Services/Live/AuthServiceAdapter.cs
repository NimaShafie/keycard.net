// Services/Live/AuthServiceAdapter.cs
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services.Live
{
    /// <summary>
    /// Live adapter that calls the backend API for authentication.
    /// Ensures all required fields are non-null before sending to backend.
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
                // ✅ CRITICAL FIX: Ensure LastName is never null
                // Backend GuestSignupCommand expects LastName to be string? but
                // AuthService.GuestSignupAsync assumes it's not null
                var safeLastName = string.IsNullOrWhiteSpace(lastName) ? "User" : lastName.Trim();
                var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "Guest" : firstName.Trim();

                var payload = new
                {
                    Email = email.Trim(),
                    FirstName = safeFirstName,
                    LastName = safeLastName,  // ✅ Never null or empty
                    Password = password
                };

                Console.WriteLine($"Attempting staff registration for {username} at api/Auth/guest/signup");
                Console.WriteLine($"Payload: Email={payload.Email}, FirstName={payload.FirstName}, LastName={payload.LastName}");
                Console.WriteLine($"Client BaseAddress: {Client.BaseAddress}");
                Console.WriteLine($"Full URL will be: {Client.BaseAddress}api/Auth/guest/signup");

                var response = await Client.PostAsJsonAsync("api/Auth/guest/signup", payload, ct);

                Console.WriteLine($"Registration response: {(int)response.StatusCode} {response.ReasonPhrase}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Registration successful!");
                    return (true, null);
                }

                // Try to read error message from response
                string? errorMessage = null;
                try
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    Console.WriteLine($"Error response content: {errorContent}");

                    // Try to parse error message from JSON
                    if (!string.IsNullOrWhiteSpace(errorContent))
                    {
                        using var doc = JsonDocument.Parse(errorContent);
                        if (doc.RootElement.TryGetProperty("message", out var msgElement))
                        {
                            errorMessage = msgElement.GetString();
                        }
                        else if (doc.RootElement.TryGetProperty("title", out var titleElement))
                        {
                            errorMessage = titleElement.GetString();
                        }
                        else
                        {
                            errorMessage = errorContent;
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors
                }

                errorMessage ??= $"Registration failed with status {(int)response.StatusCode}";
                Console.WriteLine($"Registration failed: {errorMessage}");
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (false, $"Unable to connect to the server. Please check your connection and try again. Details: {ex.Message}");
            }
        }

        public void Logout()
        {
            IsAuthenticated = false;
            DisplayName = string.Empty;
        }
    }
}
