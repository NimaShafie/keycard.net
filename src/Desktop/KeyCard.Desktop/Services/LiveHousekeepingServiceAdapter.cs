// Services/LiveHousekeepingServiceAdapter.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services
{
    /// <summary>
    /// Live HK adapter. Calls backend endpoints; never throws to UI. Returns safe defaults on failure.
    /// </summary>
    public sealed class LiveHousekeepingServiceAdapter : IHousekeepingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LiveHousekeepingServiceAdapter> _logger;

        public LiveHousekeepingServiceAdapter(
            IHttpClientFactory httpClientFactory,
            ILogger<LiveHousekeepingServiceAdapter> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<IReadOnlyList<Room>> GetRoomsAsync()
        {
            try
            {
                // TODO: replace with your real rooms endpoint when ready
                var list = await Client.GetFromJsonAsync<List<Room>>("api/admin/rooms");
                return (IReadOnlyList<Room>)(list ?? new List<Room>());
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Backend unreachable during GetRoomsAsync; returning empty.");
                return Array.Empty<Room>();
            }
        }

        public async Task<IReadOnlyList<HousekeepingTask>> GetTasksAsync()
        {
            try
            {
                var list = await Client.GetFromJsonAsync<List<HousekeepingTask>>("api/admin/housekeeping");
                return (IReadOnlyList<HousekeepingTask>)(list ?? new List<HousekeepingTask>());
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Backend unreachable during GetTasksAsync; returning empty.");
                return Array.Empty<HousekeepingTask>();
            }
        }

        public async Task<bool> UpdateRoomStatusAsync(int roomNumber, RoomStatus status)
        {
            try
            {
                using var resp = await Client.PostAsJsonAsync(
                    $"api/admin/rooms/{roomNumber}/status",
                    new { status = status.ToString() });
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Backend unreachable during UpdateRoomStatusAsync.");
                return false;
            }
        }

        public async Task<bool> UpdateTaskStatusAsync(string taskId, HkTaskStatus status)
        {
            try
            {
                using var resp = await Client.PostAsJsonAsync(
                    $"api/admin/housekeeping/{Uri.EscapeDataString(taskId)}/status",
                    new { status = status.ToString() });
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Backend unreachable during UpdateTaskStatusAsync.");
                return false;
            }
        }
    }
}
