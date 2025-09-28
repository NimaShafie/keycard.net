using KeyCard.Application.UseCases.CheckIn;
using KeyCard.Contracts.Bookings;

namespace KeyCard.Infrastructure.UseCases.CheckIn;

/// <summary>
/// Stub: in future, validate booking, assign room, change state, persist, emit events.
/// </summary>
public sealed class CheckInHandler : ICheckInHandler
{
    public Task<CheckInResponse> HandleAsync(CheckInRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        // placeholder response
        return Task.FromResult(new CheckInResponse(request.BookingId, "101", now));
    }
}
