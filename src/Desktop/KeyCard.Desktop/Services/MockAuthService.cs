// Services/MockAuthService.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services
{
    public sealed class MockAuthService : IAuthService
    {
        public bool IsAuthenticated { get; private set; }
        public string? DisplayName { get; private set; }
        public event EventHandler? StateChanged;

        public Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            IsAuthenticated = true;
            DisplayName = string.IsNullOrWhiteSpace(username) ? "Mock Staff" : username;
            StateChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        public Task<bool> LoginMockAsync(CancellationToken ct = default)
        {
            IsAuthenticated = true;
            DisplayName = "Mock Staff";
            StateChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        // aliases
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
