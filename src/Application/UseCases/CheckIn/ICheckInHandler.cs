using KeyCard.Contracts.Bookings;

namespace KeyCard.Application.UseCases.CheckIn;

/// <summary>Check-in use-case contract.</summary>
public interface ICheckInHandler
{
    Task<CheckInResponse> HandleAsync(CheckInRequest request, CancellationToken ct);
}
