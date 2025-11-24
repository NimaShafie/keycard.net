// Models/Booking.cs
namespace KeyCard.Desktop.Models;

public sealed record Booking
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public string ConfirmationCode { get; init; } = "";
    public string GuestFirstName { get; init; } = "";
    public string GuestLastName { get; init; } = "";
    public int RoomNumber { get; init; }
    public string RoomType { get; init; } = "Regular Room"; // "Regular Room" | "King Room" | "Luxury Room"
    public DateOnly CheckInDate { get; init; }
    public DateOnly CheckOutDate { get; init; }
    public string Status { get; init; } = "Reserved";

    // Computed property for full name (backward compatibility)
    public string GuestName => string.IsNullOrWhiteSpace(GuestFirstName)
        ? GuestLastName
        : string.IsNullOrWhiteSpace(GuestLastName)
            ? GuestFirstName
            : $"{GuestFirstName} {GuestLastName}";

    public DateOnly CheckIn => CheckInDate;
    public DateOnly CheckOut => CheckOutDate;

    // (optional display strings if XAML expects text)
    // public string CheckInText  => CheckInDate.ToString("MMM d");
    // public string CheckOutText => CheckOutDate.ToString("MMM d");
}
