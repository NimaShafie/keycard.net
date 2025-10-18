// Services/Mock/RoomsService.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services.Mock
{
    public sealed class RoomsService : IRoomsService
    {
        // Simple, predictable catalog for mock
        private static readonly List<RoomOption> _catalog =
        [
            new RoomOption { Number = 101, Type = "Regular Room" },
            new RoomOption { Number = 102, Type = "Regular Room" },
            new RoomOption { Number = 201, Type = "King Room"    },
            new RoomOption { Number = 202, Type = "King Room"    },
            new RoomOption { Number = 301, Type = "Luxury Room"  },
            new RoomOption { Number = 302, Type = "Luxury Room"  },
        ];

        public Task<IReadOnlyList<RoomOption>> GetRoomOptionsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RoomOption>>(_catalog);

        public Task<bool> ExistsAsync(int roomNumber, CancellationToken ct = default)
            => Task.FromResult(_catalog.Any(r => r.Number == roomNumber));
    }
}
