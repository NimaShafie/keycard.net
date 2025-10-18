// Services/Mock/BookingService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services.Mock;

public sealed class BookingService : IBookingService
{
    // Keep a single in-memory snapshot for the app lifetime
    private static readonly Booking[] _data = CreateSeed();

    // CA1859: return Booking[] instead of IReadOnlyList<Booking>
    private static Booking[] CreateSeed()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new[]
        {
            new Booking
            {
                Id = Guid.NewGuid(),
                ConfirmationCode = "ABC123",
                GuestLastName = "Shafie",
                RoomNumber = 101,
                CheckInDate = today,
                CheckOutDate = today.AddDays(2),
                Status = "Reserved"
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                ConfirmationCode = "ZZZ999",
                GuestLastName = "Joshi",
                RoomNumber = 202,
                CheckInDate = today,
                CheckOutDate = today.AddDays(3),
                Status = "Reserved"
            },
        };
    }

    public Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Booking>)_data);

    public Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default)
        => Task.FromResult(
            _data.FirstOrDefault(b =>
                string.Equals(b.ConfirmationCode, code, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var arrivals = _data.Where(b => b.CheckInDate == today).ToArray(); // array is fine; returned as IReadOnlyList
        return Task.FromResult<IReadOnlyList<Booking>>(arrivals);
    }

    public Task<Booking?> FindBookingByCodeAsync(string code, CancellationToken ct = default)
        => GetByCodeAsync(code, ct);

    public Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default)
        => Task.FromResult(true);
}
