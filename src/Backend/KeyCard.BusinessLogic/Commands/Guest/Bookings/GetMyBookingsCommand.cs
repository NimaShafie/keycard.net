using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Bookings
{
    public record GetMyBookingsCommand(int GuestId) : Request, IRequest<List<BookingViewModel>>;

    public class GetMyBookingsCommandHandler : IRequestHandler<GetMyBookingsCommand, List<BookingViewModel>>
    {
        private readonly IBookingService _bookingService;
        public GetMyBookingsCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }
        public async Task<List<BookingViewModel>> Handle(GetMyBookingsCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.GetBookingsByGuestIdAsync(command, cancellationToken);
        }
    }
}
