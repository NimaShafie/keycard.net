using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Bookings
{
    public record GetAllBookingsCommand(
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        string? Status = null,
        string? GuestName = null
    ) : Request, IRequest<List<BookingViewModel>>;

    public class GetAllBookingsCommandHandler : IRequestHandler<GetAllBookingsCommand, List<BookingViewModel>>
    {
        private readonly IBookingService _bookingService;
        public GetAllBookingsCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<List<BookingViewModel>> Handle(GetAllBookingsCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.GetAllBookingsAsync(command, cancellationToken);
        }
    }
}
