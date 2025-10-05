using KeyCard.BusinessLogic.Commands.Bookings;
using KeyCard.BusinessLogic.ViewModels;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface IBookingService
    {
        Task<BookingDto> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken);
        Task<BookingDto> GetBookingByIdAsync(GetBookingByIdCommand command, CancellationToken cancellationToken);
        Task<List<BookingDto>> GetAllBookingsAsync(GetAllBookingsCommand command, CancellationToken cancellationToken);
        Task<bool> CancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken);

    }
}
