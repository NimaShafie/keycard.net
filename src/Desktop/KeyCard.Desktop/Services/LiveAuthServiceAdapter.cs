// Services/LiveAuthServiceAdapter.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeyCard.Desktop.Services
{
    /// <summary>
    /// Live Auth adapter. Calls backend; never throws to UI. On failures returns false and raises StateChanged if needed.
    /// </summary>
    public sealed class LiveAuthServiceAdapter : IAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LiveAuthServiceAdapter> _logger;
        private readonly ApiRoutesOptions _routes;

        public LiveAuthServiceAdapter(
            IHttpClientFactory httpClientFactory,
            IOptions<ApiRoutesOptions> routes,
            ILogger<LiveAuthServiceAdapter> logger)
        {
            _httpClientFactory = httpClientFactory;
            _routes = routes.Value;
            _logger = logger;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public bool IsAuthenticated { get; private set; }
        public string DisplayName { get; private set; } = string.Empty;

        public event EventHandler? StateChanged;

        private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

        public async Task<bool> SignInAsync(string userOrEmail, string password, CancellationToken ct = default)
        {
            var route = _routes.StaffLogin ?? "api/auth/login";
            try
            {
                var payload = new { usernameOrEmail = userOrEmail, password };
                using var resp = await Client.PostAsJsonAsync(route, payload, ct);

                if (resp.IsSuccessStatusCode)
                {
                    // TODO: extract display name from response when backend shape is defined.
                    IsAuthenticated = true;
                    DisplayName = string.IsNullOrWhiteSpace(userOrEmail) ? "User" : userOrEmail;
                    RaiseStateChanged();
                    return true;
                }

                if (resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogInformation("Login unauthorized for {User}.", userOrEmail);
                    return false;
                }

                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Login failed: {Code} {Body}", (int)resp.StatusCode, body);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backend unreachable during SignInAsync.");
                return false;
            }
        }

        // Keep both method names supported by IAuthService. Delegate to SignInAsync.
        public Task<bool> LoginAsync(string userOrEmail, string password, CancellationToken ct = default)
            => SignInAsync(userOrEmail, password, ct);

        // Live adapter should not allow mock login, but must satisfy interface: return false (no-op).
        public Task<bool> LoginMockAsync(CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> SignInMockAsync(CancellationToken ct = default) => Task.FromResult(false);

        public void Logout()
        {
            IsAuthenticated = false;
            DisplayName = string.Empty;
            RaiseStateChanged();
        }
    }
}
