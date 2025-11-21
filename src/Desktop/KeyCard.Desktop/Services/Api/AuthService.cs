// Services/Api/AuthService.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services.Api
{
    /// <summary>
    /// API-based authentication service (transitional implementation).
    /// Note: In Live mode, AuthServiceAdapter is used instead.
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        public bool IsAuthenticated { get; private set; }
        public string DisplayName { get; private set; } = string.Empty;  // ✅ Fixed: non-nullable

        public event EventHandler? StateChanged;

        public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            // Transitional: succeed if both provided
            await Task.Yield();
            IsAuthenticated = !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
            DisplayName = IsAuthenticated ? username : string.Empty;  // ✅ Fixed: always returns string
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

        public Task<bool> SignInAsync(string username, string password, CancellationToken ct = default)
            => LoginAsync(username, password, ct);

        public Task<bool> SignInMockAsync(CancellationToken ct = default)
            => LoginMockAsync(ct);

        public Task<(bool success, string? errorMessage)> RegisterStaffAsync(
            string username,
            string email,
            string password,
            string firstName,
            string lastName,
            string? employeeId = null,
            CancellationToken ct = default)
        {
            // Transitional: return success
            return Task.FromResult((true, (string?)null));
        }

        public void Logout()
        {
            IsAuthenticated = false;
            DisplayName = string.Empty;  // ✅ Fixed: set to empty string instead of null
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
