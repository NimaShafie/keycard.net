namespace KeyCard.Contracts.Bookings;

/// <summary>Shared DTOs between API, Web, Desktop.</summary>
public record AvailabilityRequest(DateOnly From, DateOnly To, int Guests);
public record AvailabilityResponse(string RoomNumber, string RoomType);

public record CheckInRequest(Guid BookingId);
public record CheckInResponse(Guid BookingId, string RoomNumber, DateTime CheckedInAtUtc);
