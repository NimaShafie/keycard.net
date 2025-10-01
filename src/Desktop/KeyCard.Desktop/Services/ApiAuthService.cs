// Services/ApiAuthService.cs
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services;

public sealed class ApiAuthService : IAuthService
{
    public Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password));

    public Task LogoutAsync(CancellationToken ct = default) => Task.CompletedTask;
}
