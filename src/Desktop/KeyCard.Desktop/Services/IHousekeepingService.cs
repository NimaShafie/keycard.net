// Services/IHousekeepingService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services
{
    public interface IHousekeepingService
    {
        Task<IReadOnlyList<Room>> GetRoomsAsync();
        Task<IReadOnlyList<HousekeepingTask>> GetTasksAsync();
        Task<bool> UpdateRoomStatusAsync(int roomNumber, RoomStatus status);
        Task<bool> UpdateTaskStatusAsync(string taskId, HkTaskStatus status);
    }
}
