// Services/Api/IBackendClient.cs
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Options;

namespace KeyCard.Desktop.Services.Api
{
    public sealed class BackendClient : IBackendClient
    {
        private readonly ApiOptions _opts;
        private readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private string? _jwt;

        public BackendClient(ApiOptions opts)
        {
            _opts = opts;
            Http = new HttpClient
            {
                BaseAddress = new Uri(opts.BaseUrl, UriKind.Absolute),
                Timeout = opts.Timeout
            };
            // trust dev certs are handled by OS (dotnet dev-certs https --trust)
        }

        public HttpClient Http { get; }

        public async Task<string> HealthAsync(CancellationToken ct = default)
        {
            var r = await Http.GetAsync("/api/v1/Health", ct);
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadAsStringAsync(ct);
        }

        public async Task<string> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            // Adjust payload property names to match your AuthController login model
            var payload = new { email, password };
            var r = await Http.PostAsJsonAsync("/api/Auth/login", payload, _json, ct);
            r.EnsureSuccessStatusCode();
            var json = await r.Content.ReadAsStringAsync(ct);

            // Expected shape: { "token": "..." } or { "data": { "token": "..." } } depending on your wrapper
            // Handle both:
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? token = null;
            if (root.TryGetProperty("token", out var tokenEl))
                token = tokenEl.GetString();
            else if (root.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("token", out var tokenEl2))
                token = tokenEl2.GetString();

            _jwt ??= token;
            if (!string.IsNullOrWhiteSpace(_jwt))
            {
                Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
            }

            return json; // raw for debugging in UI logs
        }

        public async Task<string> GetAdminBookingsRawAsync(CancellationToken ct = default)
        {
            var r = await Http.GetAsync("/api/admin/Bookings", ct);
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadAsStringAsync(ct);
        }
    }
}
