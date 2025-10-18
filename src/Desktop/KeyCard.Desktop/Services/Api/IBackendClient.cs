// Services/Api/IBackendClient.cs
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Services.Api
{
    public interface IBackendClient
    {
        Task<string> HealthAsync(CancellationToken ct = default);
        Task<string> LoginAsync(string email, string password, CancellationToken ct = default);
        Task<string> GetAdminBookingsRawAsync(CancellationToken ct = default);
        HttpClient Http { get; } // exposed for advanced calls if needed
    }
}
