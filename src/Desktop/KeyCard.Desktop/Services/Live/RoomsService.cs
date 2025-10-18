// Services/Live/RoomsService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services.Live
{
    public sealed class RoomsService : IRoomsService
    {
        private readonly HttpClient _http;

        public RoomsService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<IReadOnlyList<RoomOption>> GetRoomOptionsAsync(CancellationToken ct = default)
        {
            // Adjust path if your base address already includes /api
            var list = await _http.GetFromJsonAsync<List<RoomOption>>("/api/guest/Rooms/room-options", ct)
                       ?? new List<RoomOption>();
            return list;
        }

        public async Task<bool> ExistsAsync(int roomNumber, CancellationToken ct = default)
        {
            var rooms = await GetRoomOptionsAsync(ct);
            for (int i = 0; i < rooms.Count; i++)
                if (rooms[i].Number == roomNumber)
                    return true;
            return false;
        }
    }
}
