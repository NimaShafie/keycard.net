// Models/Booking.cs
namespace KeyCard.Desktop.Models;

public sealed record Booking
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; } // alias used by FrontDesk VM
    public string ConfirmationCode { get; init; } = "";
    public string GuestLastName { get; init; } = "";
    public int RoomNumber { get; init; }
    public DateOnly CheckInDate { get; init; }
    public DateOnly CheckOutDate { get; init; }
    public string Status { get; init; } = "Reserved";
}
