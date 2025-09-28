using KeyCard.Application.Abstractions;
using KeyCard.Contracts.Bookings;
using KeyCard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.Services;

/// <summary>Trivial "availability" impl: returns all vacant rooms.</summary>
public sealed class AvailabilityService : IAvailabilityService
{
    private readonly KeyCardDbContext _db;
    public AvailabilityService(KeyCardDbContext db) => _db = db;

    public async Task<IEnumerable<AvailabilityResponse>> SearchAsync(AvailabilityRequest request, CancellationToken ct)
    {
        // TODO: filter by dates/guests once Booking/Inventory are modeled.
        return await _db.Rooms
            .Where(r => r.State == Domain.Rooms.RoomState.Vacant)
            .Select(r => new AvailabilityResponse(r.Number, r.Type))
            .ToListAsync(ct);
    }
}
