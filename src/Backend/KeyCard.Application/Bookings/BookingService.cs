using KeyCard.Contracts.Bookings;
using KeyCard.Domain.Bookings;
using KeyCard.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KeyCard.Application.Bookings;

public sealed class BookingService(AppDbContext db) : IBookingService
{
    public async Task<BookingDto?> GetByCodeAsync(string code, CancellationToken ct)
        => await db.Bookings
           .Where(b => b.ConfirmationCode == code)
           .Select(Map)
           .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BookingDto>> ListAsync(CancellationToken ct)
        => await db.Bookings.Select(Map).ToListAsync(ct);

    private static BookingDto Map(Booking b) => new(
        b.Id, b.ConfirmationCode, b.GuestLastName, b.RoomNumber, b.CheckInDate, b.CheckOutDate, b.Status);
}
