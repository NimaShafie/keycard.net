// Services/Mock/AuthService.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services.Mock
{
    /// <summary>
    /// Mock authentication service for development and testing.
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        public bool IsAuthenticated { get; private set; }
        public string DisplayName { get; private set; } = string.Empty;

        public event EventHandler? StateChanged;

        // ---- canonical ----
        public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            // Mock login - succeed if both provided
            await Task.Yield();
            IsAuthenticated = !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
            DisplayName = IsAuthenticated ? username : string.Empty;
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

        /// <summary>
        /// Mock registration - always succeeds in mock mode.
        /// </summary>
        public Task<(bool success, string? errorMessage)> RegisterStaffAsync(
            string username,
            string email,
            string password,
            string firstName,
            string lastName,
            string? employeeId = null,
            CancellationToken ct = default)
        {
            // Mock mode: always succeed
            return Task.FromResult((true, (string?)null));
        }

        public void Logout()
        {
            IsAuthenticated = false;
            DisplayName = string.Empty;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
