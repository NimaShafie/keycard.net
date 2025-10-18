// Services/LiveAuthServiceAdapter.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeyCard.Desktop.Services
{
    /// <summary>
    /// Live Auth adapter. Calls backend; never throws to UI. On failures returns false and logs.
    /// </summary>
    public sealed class LiveAuthServiceAdapter : IAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LiveAuthServiceAdapter> _logger;
        private readonly ApiRoutesOptions _routes;

        private string? _jwt; // ✅ keep last token

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

        private void RaiseStateChangedIfChanged(bool newIsAuthed, string? newDisplayName = null)
        {
            var changed = newIsAuthed != IsAuthenticated
                          || (newDisplayName is not null && !string.Equals(newDisplayName, DisplayName, StringComparison.Ordinal));
            if (!changed) return;

            IsAuthenticated = newIsAuthed;
            if (newDisplayName is not null) DisplayName = newDisplayName;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> SignInAsync(string username, string password, CancellationToken ct = default)
        {
            var route = _routes.StaffLogin ?? "api/auth/login";

            try
            {
                var payload = new { username, password };
                using var resp = await Client.PostAsJsonAsync(route, payload, ct).ConfigureAwait(false);

                if (resp.IsSuccessStatusCode)
                {
                    // Try to pull display name AND token (if present)
                    var (name, token) = await TryExtractNameAndTokenAsync(resp, username, ct).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        _jwt = token;
                        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
                        _logger.LogInformation("JWT attached to HttpClient Authorization header.");
                    }

                    RaiseStateChangedIfChanged(true, name);
                    return true;
                }

                if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    _logger.LogInformation("Login unauthorized for {User}.", username);
                    return false;
                }

                var body = await SafeReadBodyAsync(resp, ct).ConfigureAwait(false);
                _logger.LogWarning("Login failed [{Status}]: {Body}", (int)resp.StatusCode, body);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Backend unreachable during SignInAsync for {User}.", username);
                return false;
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation(ex, "SignInAsync canceled for {User}.", username);
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "SignInAsync timed out for {User}.", username);
                return false;
            }
#pragma warning disable CA1031 // Do not catch general exception types — by design we never throw to UI
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during SignInAsync for {User}.", username);
                return false;
            }
#pragma warning restore CA1031
        }

        // Keep both method names supported by IAuthService. Delegate to SignInAsync.
        public Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
            => SignInAsync(username, password, ct);

        // Live adapter does not allow mock login; satisfy interface.
        public Task<bool> LoginMockAsync(CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> SignInMockAsync(CancellationToken ct = default) => Task.FromResult(false);

        public void Logout()
        {
            _jwt = null;
            Client.DefaultRequestHeaders.Authorization = null;
            RaiseStateChangedIfChanged(false, string.Empty);
        }

        private static async Task<string> SafeReadBodyAsync(HttpResponseMessage resp, CancellationToken ct)
        {
            try
            {
                return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                return "<unreadable body>";
            }
        }

        private static async Task<(string displayName, string? token)> TryExtractNameAndTokenAsync(
            HttpResponseMessage resp, string fallback, CancellationToken ct)
        {
            string? name = null;
            string? token = null;

            try
            {
                await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                var root = doc.RootElement;

                static string? pickName(JsonElement e)
                {
                    if (e.ValueKind != JsonValueKind.Object) return null;

                    if (e.TryGetProperty("displayName", out var dn) && dn.ValueKind == JsonValueKind.String)
                        return dn.GetString();
                    if (e.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                        return n.GetString();
                    if (e.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.String)
                        return u.GetString();

                    return null;
                }

                // Token can be { token: "..." } or { data: { token: "..." } }
                static string? pickToken(JsonElement e)
                {
                    if (e.ValueKind != JsonValueKind.Object) return null;
                    if (e.TryGetProperty("token", out var t) && t.ValueKind == JsonValueKind.String)
                        return t.GetString();
                    return null;
                }

                name = pickName(root);
                token = pickToken(root);

                if (root.TryGetProperty("data", out var data))
                {
                    name ??= pickName(data);
                    token ??= pickToken(data);
                }
            }
            catch
            {
                // ignore parse errors; fall back below
            }

            return (string.IsNullOrWhiteSpace(name) ? (string.IsNullOrWhiteSpace(fallback) ? "User" : fallback) : name, token);
        }
    }
}
