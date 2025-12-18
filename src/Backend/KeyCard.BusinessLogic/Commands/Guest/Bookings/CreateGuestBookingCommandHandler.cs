using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Bookings
{
    public class CreateGuestBookingCommandHandler : IRequestHandler<CreateGuestBookingCommand, BookingViewModel>
    {
        private readonly IBookingService _bookingService;

        public CreateGuestBookingCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<BookingViewModel> Handle(CreateGuestBookingCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.CreateGuestBookingAsync(command, cancellationToken);
        }
    }
}

