// Services/IAuthService.cs
using System.Threading.Tasks;
using KeyCard.Desktop.Models;
namespace KeyCard.Desktop.Services;

public interface IAuthService
{
    Task<UserSession?> LoginAsync(string username, string password);
    Task LogoutAsync();
}
