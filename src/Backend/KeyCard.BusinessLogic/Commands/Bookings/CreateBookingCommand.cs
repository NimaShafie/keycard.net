using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Bookings
{
    public record CreateBookingCommand 
    (
        int GuestProfileId,
        int RoomId,
        DateTime CheckInDate,
        DateTime CheckOutDate,
        int Adults,
        int Children,
        bool IsPrepaid
    ) : Request, IRequest<BookingViewModel>;

    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingViewModel>
    {
        private readonly IBookingService _bookingService;
        public CreateBookingCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<BookingViewModel> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.CreateBookingAsync(command, cancellationToken);
        }
    }
}
