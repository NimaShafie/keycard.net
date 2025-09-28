// Services/IBookingService.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services;

public interface IBookingService
{
    Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct = default);
    Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync(CancellationToken ct = default);
    Task<Booking?> FindBookingByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default);
    Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default);
}
