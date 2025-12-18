using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Bookings
{
    public record CreateGuestBookingCommand(
        int RoomTypeId,
        DateTime CheckInDate,
        DateTime CheckOutDate,
        int Adults,
        int Children,
        bool IsPrepaid,
        // Guest info for booking without login
        string? GuestEmail = null,
        string? GuestFirstName = null,
        string? GuestLastName = null
    ) : Request, IRequest<BookingViewModel>;

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

