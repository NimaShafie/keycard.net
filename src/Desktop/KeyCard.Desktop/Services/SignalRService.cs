// Services/SignalRService.cs
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace KeyCard.Desktop.Services;

public interface ISignalRService
{
    HubConnection BookingsHub { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}

public sealed class SignalRService : ISignalRService
{
    public HubConnection BookingsHub { get; }

    public SignalRService(string apiBaseUrl)
    {
        var baseUrl = apiBaseUrl.TrimEnd('/');
        BookingsHub = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hub/bookings")
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (BookingsHub.State == HubConnectionState.Disconnected)
            await BookingsHub.StartAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (BookingsHub.State != HubConnectionState.Disconnected)
            await BookingsHub.StopAsync(ct);
    }
}
