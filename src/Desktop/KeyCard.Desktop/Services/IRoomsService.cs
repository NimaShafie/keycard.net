// Services/IRoomsService.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services
{
    public interface IRoomsService
    {
        /// <summary>Returns the full list of rooms and their types.</summary>
        Task<IReadOnlyList<RoomOption>> GetRoomOptionsAsync(CancellationToken ct = default);

        /// <summary>Convenience check for existence.</summary>
        Task<bool> ExistsAsync(int roomNumber, CancellationToken ct = default);
    }
}
