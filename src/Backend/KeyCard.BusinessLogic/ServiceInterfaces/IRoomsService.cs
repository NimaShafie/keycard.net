using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.Commands.Guest.Rooms;
using KeyCard.BusinessLogic.ViewModels.Rooms;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface IRoomsService
    {
        Task<RoomOptionsViewModel> GetRoomOptionsAsync(GetRoomOptionsCommand command, CancellationToken cancellationToken);
    }
}
