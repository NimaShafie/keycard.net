// Services/SignalRService.cs
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

namespace KeyCard.Desktop.Services
{
    public interface ISignalRService
    {
        HubConnection BookingsHub { get; }
        Task StartAsync(CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Existing implementation that derives the hub URL from an API base URL.
    /// Kept intact for back-compat and other callers that pass an API base.
    /// </summary>
    public sealed class SignalRService : ISignalRService
    {
        public HubConnection BookingsHub { get; }

        /// <param name="apiBaseUrl">API base URL, e.g. https://api.example.com</param>
        public SignalRService(string apiBaseUrl)
        {
            var baseUrl = (apiBaseUrl ?? string.Empty).TrimEnd('/');
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
}

namespace KeyCard.Desktop.Services.Live
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.SignalR.Client;

    /// <summary>
    /// LIVE-targeted implementation that accepts the full hub URL directly.
    /// This is what Bootstrap.cs instantiates with env.BookingsHubUrl.
    /// </summary>
    public sealed class SignalRService : KeyCard.Desktop.Services.ISignalRService
    {
        public HubConnection BookingsHub { get; }

        /// <param name="hubUrl">
        /// Full hub URL (no transformation), e.g. https://api.example.com/hub/bookings
        /// </param>
        public SignalRService(string hubUrl)
        {
            var url = (hubUrl ?? string.Empty).Trim();
            BookingsHub = new HubConnectionBuilder()
                .WithUrl(url)
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
}
