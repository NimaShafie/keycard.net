// Services/ApiHousekeepingService.cs
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services
{
    public sealed class ApiHousekeepingService : IHousekeepingService
    {
        private readonly HttpClient _http;
        public ApiHousekeepingService(HttpClient http) => _http = http;

        public Task<IReadOnlyList<Room>> GetRoomsAsync()
            => _http.GetFromJsonAsync<IReadOnlyList<Room>>("/api/housekeeping/rooms")!;

        public Task<IReadOnlyList<HousekeepingTask>> GetTasksAsync()
            => _http.GetFromJsonAsync<IReadOnlyList<HousekeepingTask>>("/api/housekeeping/tasks")!;

        public async Task<bool> UpdateRoomStatusAsync(int roomNumber, RoomStatus status)
            => (await _http.PostAsJsonAsync("/api/housekeeping/room-status", new { roomNumber, status })).IsSuccessStatusCode;

        public async Task<bool> UpdateTaskStatusAsync(string taskId, HkTaskStatus status)
            => (await _http.PostAsJsonAsync("/api/housekeeping/task-status", new { taskId, status })).IsSuccessStatusCode;
    }
}
