// Services/BookingMapping.cs
using KeyCard.Desktop.Generated;
using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services;

internal static class BookingMapping
{
    public static Booking ToModel(this BookingDto d) => new Booking
    {
        Id = d.Id,
        BookingId = d.Id, // FrontDesk expects this name
        ConfirmationCode = d.ConfirmationCode,
        GuestLastName = d.GuestLastName,
        RoomNumber = d.RoomNumber,
        CheckInDate = d.CheckInDate,
        CheckOutDate = d.CheckOutDate,
        Status = d.Status
    };
}
