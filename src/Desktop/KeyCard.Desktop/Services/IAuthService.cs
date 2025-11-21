// Services/IAuthService.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services
{
    /// <summary>
    /// Authentication service interface supporting both login and registration.
    /// </summary>
    public interface IAuthService
    {
        bool IsAuthenticated { get; }
        string DisplayName { get; }

        event EventHandler? StateChanged;

        /// <summary>
        /// Authenticate a staff member with username and password.
        /// </summary>
        Task<bool> LoginAsync(string username, string password, CancellationToken ct = default);

        /// <summary>
        /// Mock login for development/testing purposes.
        /// </summary>
        Task<bool> LoginMockAsync(CancellationToken ct = default);

        /// <summary>
        /// Alias for LoginAsync to support alternative naming convention.
        /// </summary>
        Task<bool> SignInAsync(string username, string password, CancellationToken ct = default);

        /// <summary>
        /// Alias for LoginMockAsync to support alternative naming convention.
        /// </summary>
        Task<bool> SignInMockAsync(CancellationToken ct = default);

        /// <summary>
        /// Register a new staff member.
        /// </summary>
        /// <returns>A tuple containing success status and optional error message</returns>
        Task<(bool success, string? errorMessage)> RegisterStaffAsync(
            string username,
            string email,
            string password,
            string firstName,
            string lastName,
            string? employeeId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Sign out the current user.
        /// </summary>
        void Logout();
    }
}
