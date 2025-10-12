using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Admin.Bookings
{
    public record GetBookingByIdCommand (int BookingId) : Request, IRequest<BookingViewModel>;

    public class GetBookingByIdCommandHandler : IRequestHandler<GetBookingByIdCommand, BookingViewModel>
    {
        private readonly IBookingService _bookingService;
        public GetBookingByIdCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<BookingViewModel> Handle(GetBookingByIdCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.GetBookingByIdAsync(command, cancellationToken);
        }
    }
}
