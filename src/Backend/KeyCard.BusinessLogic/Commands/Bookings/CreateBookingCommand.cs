using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;
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
    ) : IRequest<BookingDto>;

    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
    {
        private readonly IBookingService _bookingService;
        public CreateBookingCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<BookingDto> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.CreateBookingAsync(command, cancellationToken);
        }
    }
}
