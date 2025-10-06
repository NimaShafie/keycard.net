// Services/ApiAuthService.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services
{
    public sealed class ApiAuthService : IAuthService
    {
        public bool IsAuthenticated { get; private set; }
        public string? DisplayName { get; private set; }

        public event EventHandler? StateChanged;

        // ---- canonical ----
        public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            // TODO: call real API. For now, succeed if both provided.
            await Task.Yield();
            IsAuthenticated = !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
            DisplayName = IsAuthenticated ? username : null;
            StateChanged?.Invoke(this, EventArgs.Empty);
            return IsAuthenticated;
        }

        public Task<bool> LoginMockAsync(CancellationToken ct = default)
        {
            IsAuthenticated = true;
            DisplayName = "Mock Staff";
            StateChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        // ---- compatibility aliases ----
        public Task<bool> SignInAsync(string username, string password, CancellationToken ct = default)
            => LoginAsync(username, password, ct);

        public Task<bool> SignInMockAsync(CancellationToken ct = default)
            => LoginMockAsync(ct);

        public void Logout()
        {
            IsAuthenticated = false;
            DisplayName = null;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
