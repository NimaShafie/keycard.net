using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Bookings
{
    public record CreateGuestBookingCommand(
        int RoomTypeId,
        DateTime CheckInDate,
        DateTime CheckOutDate,
        int Adults,
        int Children,
        bool IsPrepaid,
        // Guest info for booking without login
        string? GuestEmail = null,
        string? GuestFirstName = null,
        string? GuestLastName = null
    ) : Request, IRequest<BookingViewModel>;
}

