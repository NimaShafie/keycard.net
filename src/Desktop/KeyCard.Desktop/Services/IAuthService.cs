// Services/IAuthService.cs
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
}
