// Models/Booking.cs
namespace KeyCard.Desktop.Models;

public sealed record Booking
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public string ConfirmationCode { get; init; } = "";
    public string GuestLastName { get; init; } = "";
    public int RoomNumber { get; init; }
    public DateOnly CheckInDate { get; init; }
    public DateOnly CheckOutDate { get; init; }
    public string Status { get; init; } = "Reserved";

    public string GuestName => GuestLastName;

    public DateOnly CheckIn => CheckInDate;
    public DateOnly CheckOut => CheckOutDate;

    // (optional display strings if XAML expects text)
    // public string CheckInText  => CheckInDate.ToString("MMM d");
    // public string CheckOutText => CheckOutDate.ToString("MMM d");
}
