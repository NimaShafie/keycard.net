using KeyCard.Contracts.Bookings;

namespace KeyCard.Application.Bookings;

public interface IBookingService
{
    Task<BookingDto?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<BookingDto>> ListAsync(CancellationToken ct);
}
