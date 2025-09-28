using KeyCard.Contracts.Bookings;

namespace KeyCard.Application.Abstractions;

/// <summary>Application layer exposes use-cases as interfaces.</summary>
public interface IAvailabilityService
{
    Task<IEnumerable<AvailabilityResponse>> SearchAsync(AvailabilityRequest request, CancellationToken ct);
}
