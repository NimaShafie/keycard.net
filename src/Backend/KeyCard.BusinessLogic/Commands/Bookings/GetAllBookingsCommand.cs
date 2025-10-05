using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Bookings
{
    public record GetAllBookingsCommand(
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        string? Status = null,
        string? GuestName = null
    ) : IRequest<List<BookingDto>>;

    public class GetAllBookingsCommandHandler : IRequestHandler<GetAllBookingsCommand, List<BookingDto>>
    {
        private readonly IBookingService _bookingService;
        public GetAllBookingsCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<List<BookingDto>> Handle(GetAllBookingsCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.GetAllBookingsAsync(command, cancellationToken);
        }
    }
}
