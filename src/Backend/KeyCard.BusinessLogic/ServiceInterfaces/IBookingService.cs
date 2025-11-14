using KeyCard.BusinessLogic.Commands.Admin.Bookings;
using KeyCard.BusinessLogic.Commands.Guest.Bookings;
using KeyCard.BusinessLogic.ViewModels.Booking;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface IBookingService
    {

        //admin
        Task<BookingViewModel> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken);
        Task<BookingViewModel> GetBookingByIdAsync(GetBookingByIdCommand command, CancellationToken cancellationToken);
        Task<List<BookingViewModel>> GetAllBookingsAsync(GetAllBookingsCommand command, CancellationToken cancellationToken);
        Task<bool> CancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken);
        Task<bool> CheckInBookingAsync(CheckInBookingCommand command, CancellationToken cancellationToken);
        Task<bool> CheckOutBookingAsync(CheckOutBookingCommand command, CancellationToken cancellationToken);

        //guest
        Task<List<BookingViewModel>> GetBookingsByGuestIdAsync(GetMyBookingsCommand command, CancellationToken cancellationToken);
        Task<string> GetBookingStatusByIdAsync(GetBookingStatusByIdCommand command, CancellationToken cancellationToken);
        Task<BookingViewModel> GuestCheckInAsync(GuestCheckInCommand command, CancellationToken cancellationToken);
        Task<BookingViewModel> LookUpBookingAsync(LookupBookingCommand command, CancellationToken cancellationToken);   
    }
}
