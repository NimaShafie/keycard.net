using KeyCard.BusinessLogic.Commands.Bookings;
using KeyCard.BusinessLogic.ViewModels.Booking;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface IBookingService
    {
        Task<BookingViewModel> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken);
        Task<BookingViewModel> GetBookingByIdAsync(GetBookingByIdCommand command, CancellationToken cancellationToken);
        Task<List<BookingViewModel>> GetAllBookingsAsync(GetAllBookingsCommand command, CancellationToken cancellationToken);
        Task<bool> CancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken);

    }
}
