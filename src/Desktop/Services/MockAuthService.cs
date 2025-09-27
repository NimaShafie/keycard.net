// Services/MockAuthService.cs
using System.Threading.Tasks;
using KeyCard.Desktop.Models;
namespace KeyCard.Desktop.Services;

public sealed class MockAuthService : IAuthService
{
    public Task<UserSession?> LoginAsync(string u, string p)
        => Task.FromResult<UserSession?>(u == "admin" && p == "admin" ? new("1", "Front Desk Admin", "jwt-mock") : null);
    public Task LogoutAsync() => Task.CompletedTask;
}
