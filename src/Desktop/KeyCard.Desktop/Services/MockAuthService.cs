// Services/MockAuthService.cs
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services;

public sealed class MockAuthService : IAuthService
{
    public Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
        => Task.FromResult(username == "admin" && password == "password");

    public Task LogoutAsync(CancellationToken ct = default) => Task.CompletedTask;
}
