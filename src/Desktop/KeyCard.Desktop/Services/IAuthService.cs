// src/Desktop/KeyCard.Desktop/Services/IAuthService.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services
{
    public interface IAuthService
    {
        bool IsAuthenticated { get; }
        string? DisplayName { get; }
        event EventHandler? StateChanged;

        // canonical names
        Task<bool> LoginAsync(string username, string password, CancellationToken ct = default);
        Task<bool> LoginMockAsync(CancellationToken ct = default);

        // compatibility aliases (so existing callers compile)
        Task<bool> SignInAsync(string username, string password, CancellationToken ct = default);
        Task<bool> SignInMockAsync(CancellationToken ct = default);

        void Logout();
    }
}
