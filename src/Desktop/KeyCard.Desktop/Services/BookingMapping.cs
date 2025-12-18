// Services/BookingMapping.cs
using KeyCard.Desktop.Generated;
using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services;

internal static class BookingMapping
{
    public static Booking ToModel(this BookingDto d) => new Booking
    {
        Id = (int)d.Id.GetHashCode(), // Convert Guid to int for compatibility
        ConfirmationCode = d.ConfirmationCode,
        GuestName = d.GuestLastName, // Backend uses GuestName
        RoomNumber = d.RoomNumber.ToString(),
        CheckInDate = d.CheckInDate.ToDateTime(TimeOnly.MinValue),
        CheckOutDate = d.CheckOutDate.ToDateTime(TimeOnly.MinValue),
        StatusEnum = Enum.TryParse<BookingStatus>(d.Status, ignoreCase: true, out var status) 
            ? status 
            : BookingStatus.Reserved
    };
}
