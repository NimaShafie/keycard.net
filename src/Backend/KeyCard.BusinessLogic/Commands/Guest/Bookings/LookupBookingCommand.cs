using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Bookings
{
    public record LookupBookingCommand (string Code, string Email) : IRequest<BookingViewModel>;

    public class LookupBookingCommandHandler : IRequestHandler<LookupBookingCommand, BookingViewModel>
    {
        IBookingService _bookingService;
        public LookupBookingCommandHandler(IBookingService bookingService)
        {
            this._bookingService = bookingService;
        }
        public async Task<BookingViewModel> Handle(LookupBookingCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.LookUpBookingAsync(command, cancellationToken);
        }
    }
}
