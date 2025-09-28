// Services/MockHousekeepingService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services
{
    public sealed class MockHousekeepingService : IHousekeepingService
    {
        private readonly List<Room> _rooms = Enumerable.Range(101, 20)
            .Select(i => new Room(i, i % 5 == 0 ? "Suite" : "Standard",
                i % 4 == 0 ? RoomStatus.Dirty : RoomStatus.Available))
            .ToList();

        private readonly List<HousekeepingTask> _tasks = new()
        {
            new("HK-1", 104, "Deep clean after late checkout", HkTaskStatus.Pending, DateTime.Now.AddMinutes(-30)),
            new("HK-2", 110, "Replace linens", HkTaskStatus.InProgress, DateTime.Now.AddMinutes(-10)),
            new("HK-3", 205, "Restock minibar", HkTaskStatus.Pending, DateTime.Now.AddMinutes(-5))
        };

        public Task<IReadOnlyList<Room>> GetRoomsAsync() => Task.FromResult<IReadOnlyList<Room>>(_rooms.ToList());
        public Task<IReadOnlyList<HousekeepingTask>> GetTasksAsync() => Task.FromResult<IReadOnlyList<HousekeepingTask>>(_tasks.ToList());

        public Task<bool> UpdateRoomStatusAsync(int roomNumber, RoomStatus status)
        {
            var idx = _rooms.FindIndex(r => r.Number == roomNumber);
            if (idx < 0) return Task.FromResult(false);
            _rooms[idx] = _rooms[idx] with { Status = status };
            return Task.FromResult(true);
        }

        public Task<bool> UpdateTaskStatusAsync(string taskId, HkTaskStatus status)
        {
            var idx = _tasks.FindIndex(t => t.TaskId == taskId);
            if (idx < 0) return Task.FromResult(false);
            _tasks[idx] = _tasks[idx] with { Status = status };
            return Task.FromResult(true);
        }
    }
}
