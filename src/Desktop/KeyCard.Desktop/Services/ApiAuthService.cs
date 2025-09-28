// Services/ApiAuthService.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KeyCard.Desktop.Models;
namespace KeyCard.Desktop.Services;

public sealed class ApiAuthService : IAuthService
{
    private readonly HttpClient _http;
    public ApiAuthService(HttpClient http) => _http = http;
    public async Task<UserSession?> LoginAsync(string username, string password)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/login", new { username, password });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<UserSession>();
    }
    public Task LogoutAsync() => Task.CompletedTask;
}
