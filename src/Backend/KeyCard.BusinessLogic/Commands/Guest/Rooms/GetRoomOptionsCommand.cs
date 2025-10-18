using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Rooms;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Rooms
{
    public record GetRoomOptionsCommand(
        DateOnly CheckIn,
        DateOnly CheckOut,
        int Guests = 1,
        int Rooms = 1,
        string Currency = "USD") : IRequest<RoomOptionsViewModel>;

        public class GetRoomOptionsCommandHandler : IRequestHandler<GetRoomOptionsCommand, RoomOptionsViewModel>
    {
        public IRoomsService _roomsService;
        public GetRoomOptionsCommandHandler(IRoomsService roomsService)
        {
            _roomsService = roomsService;
        }

        public async Task<RoomOptionsViewModel> Handle(GetRoomOptionsCommand command, CancellationToken cancellationToken)
        {
            return await _roomsService.GetRoomOptionsAsync(command, cancellationToken);
        }
    }
}
