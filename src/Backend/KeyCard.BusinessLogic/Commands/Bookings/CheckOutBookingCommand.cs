using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Bookings
{
    public record CheckOutBookingCommand(int BookingId) : Request, IRequest<bool>;

    public class CheckOutBookingCommandHandler : IRequestHandler<CheckOutBookingCommand, bool>
    {
        private readonly IBookingService _bookingService;

        public CheckOutBookingCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<bool> Handle(CheckOutBookingCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.CheckOutBookingAsync(command, cancellationToken);
        }
    }
}
