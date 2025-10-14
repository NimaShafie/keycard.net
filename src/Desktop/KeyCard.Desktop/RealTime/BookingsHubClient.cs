// RealTime/BookingsHubClient.cs
using System;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Services;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop.RealTime
{
    public sealed class BookingsHubClient : IAsyncDisposable
    {
        private readonly IAppEnvironment _env;
        private readonly ILogger<BookingsHubClient> _logger;
        private HubConnection? _conn;

        public event Action<string /*bookingId*/, string /*roomNumber*/>? RoomReady;

        public BookingsHubClient(IAppEnvironment env, ILogger<BookingsHubClient> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task StartAsync(string? bearerToken = null, CancellationToken ct = default)
        {
            var hubUrl = _env.BookingsHubUrl; // e.g., https://api/hubs/bookings

            _conn = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    if (!string.IsNullOrWhiteSpace(bearerToken))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(bearerToken)!;
                    }
                })
                .WithAutomaticReconnect()
                .Build();

            _conn.On<object>("RoomReady", payload =>
            {
                try
                {
                    var bookingId = payload?.GetType().GetProperty("BookingId")?.GetValue(payload)?.ToString() ?? "";
                    var roomNumber = payload?.GetType().GetProperty("RoomNumber")?.GetValue(payload)?.ToString() ?? "";
                    RoomReady?.Invoke(bookingId, roomNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process RoomReady payload");
                }
            });

            await _conn.StartAsync(ct);
            _logger.LogInformation("Connected to Bookings hub at {Url}", hubUrl);
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            if (_conn is not null)
            {
                await _conn.StopAsync(ct);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_conn is not null)
            {
                await _conn.DisposeAsync();
            }
        }
    }
}
