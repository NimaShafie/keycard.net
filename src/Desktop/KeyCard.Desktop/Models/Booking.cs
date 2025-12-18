// Models/Booking.cs
using System.Text.Json.Serialization;

namespace KeyCard.Desktop.Models;

/// <summary>
/// Booking status enum matching backend BookingStatus
/// </summary>
public enum BookingStatus
{
    Reserved = 0,
    CheckedIn = 1,
    CheckedOut = 2,
    Cancelled = 3
}

/// <summary>
/// Matches the backend BookingViewModel exactly.
/// </summary>
public sealed record Booking
{
    public int Id { get; init; }
    public string ConfirmationCode { get; init; } = "";
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    
    [JsonPropertyName("status")]
    public BookingStatus StatusEnum { get; init; } = BookingStatus.Reserved;
    
    public string GuestName { get; init; } = "";
    public string RoomNumber { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public DigitalKey? DigitalKey { get; init; }

    // Computed properties for view compatibility
    public int RoomNumberInt => int.TryParse(RoomNumber, out var num) ? num : 0;
    public DateOnly CheckIn => DateOnly.FromDateTime(CheckInDate);
    public DateOnly CheckOut => DateOnly.FromDateTime(CheckOutDate);
    
    // Status as string for UI binding compatibility
    [JsonIgnore]
    public string Status => StatusEnum.ToString();
}

/// <summary>
/// Matches backend DigitalKeyViewModel.
/// </summary>
public sealed record DigitalKey
{
    public int Id { get; init; }
    public string? KeyCode { get; init; }
    public DateTime? IssuedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
}
