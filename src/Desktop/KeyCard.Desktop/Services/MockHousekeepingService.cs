// Services/MockHousekeepingService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services
{
    public sealed class MockHousekeepingService : IHousekeepingService
    {
        private readonly List<Room> _rooms = new();
        private readonly List<HousekeepingTask> _tasks = new();

        public Task<IReadOnlyList<Room>> GetRoomsAsync()
        {
            return Task.FromResult((IReadOnlyList<Room>)_rooms);
        }

        public Task<IReadOnlyList<HousekeepingTask>> GetTasksAsync()
        {
            return Task.FromResult((IReadOnlyList<HousekeepingTask>)_tasks);
        }

        public Task<bool> UpdateRoomStatusAsync(int roomNumber, RoomStatus status)
        {
            // No-op mock
            return Task.FromResult(true);
        }

        public Task<bool> UpdateTaskStatusAsync(string taskId, HkTaskStatus status)
        {
            // No-op mock
            return Task.FromResult(true);
        }
    }
}
