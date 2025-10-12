using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Admin.Bookings
{
    public record CheckInBookingCommand(int BookingId) : Request, IRequest<bool>;

    public class CheckInBookingCommandHandler : IRequestHandler<CheckInBookingCommand, bool>
    {
        private readonly IBookingService _bookingService;

        public CheckInBookingCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<bool> Handle(CheckInBookingCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.CheckInBookingAsync(command, cancellationToken);
        }
    }
}
