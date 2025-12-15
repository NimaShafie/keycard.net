using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Bookings
{
    public record GuestCheckOutCommand(int BookingId, int GuestId) : Request, IRequest<BookingViewModel>;

    public class GuestCheckOutCommandHandler : IRequestHandler<GuestCheckOutCommand, BookingViewModel>
    {
        private readonly IBookingService _bookingService;

        public GuestCheckOutCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<BookingViewModel> Handle(GuestCheckOutCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.GuestCheckOutAsync(command, cancellationToken);
        }
    }
}

