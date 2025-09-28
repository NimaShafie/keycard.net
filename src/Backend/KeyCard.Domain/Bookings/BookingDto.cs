namespace KeyCard.Contracts.Bookings;

public record BookingDto(
    Guid Id,
    string ConfirmationCode,
    string GuestLastName,
    int RoomNumber,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    string Status);
