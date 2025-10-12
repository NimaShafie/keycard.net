using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Bookings
{
    public record GetBookingStatusByIdCommand(int BookingId) : Request, IRequest<string>;

    public class GetBookingStatusByIdCommandHandler : IRequestHandler<GetBookingStatusByIdCommand, string>
    {
        private readonly IBookingService _bookingService;
        public GetBookingStatusByIdCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<string> Handle(GetBookingStatusByIdCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.GetBookingStatusByIdAsync(command, cancellationToken);
        }
    }
}
