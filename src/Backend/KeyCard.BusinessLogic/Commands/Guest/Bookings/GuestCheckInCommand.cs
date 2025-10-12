using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Bookings
{
    public record GuestCheckInCommand(int BookingId, int GuestId) : Request, IRequest<bool>;

    public class GuestCheckInCommandHandler : IRequestHandler<GuestCheckInCommand, bool>
    {
        public IBookingService _bookingService;
        public GuestCheckInCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<bool> Handle(GuestCheckInCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.GuestCheckInAsync(command, cancellationToken);
        }
    }
}
