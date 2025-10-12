using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Admin.Bookings
{
    public record CancelBookingCommand(int BookingId) : Request, IRequest<bool>;

    public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, bool>
    {
        private readonly IBookingService _bookingService;

        public CancelBookingCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<bool> Handle(CancelBookingCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.CancelBookingAsync(command, cancellationToken);
        }
    }
}
