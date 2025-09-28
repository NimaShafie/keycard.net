// Services/MockBookingService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services;

public sealed class MockBookingService : IBookingService
{
    private static IReadOnlyList<Booking> Seed()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new[]
        {
            new Booking { Id = Guid.NewGuid(), ConfirmationCode = "ABC123", GuestLastName = "Shafie", RoomNumber = 101, CheckInDate = today, CheckOutDate = today.AddDays(2), Status = "Reserved" },
            new Booking { Id = Guid.NewGuid(), ConfirmationCode = "ZZZ999", GuestLastName = "Joshi",  RoomNumber = 202, CheckInDate = today, CheckOutDate = today.AddDays(3), Status = "Reserved" },
        };
    }

    public Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct = default)
        => Task.FromResult(Seed());

    public Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default)
        => Task.FromResult(Seed().FirstOrDefault(b => string.Equals(b.ConfirmationCode, code, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Booking>>(Seed().Where(b => b.CheckInDate == DateOnly.FromDateTime(DateTime.Today)).ToList());

    public Task<Booking?> FindBookingByCodeAsync(string code, CancellationToken ct = default)
        => GetByCodeAsync(code, ct);

    public Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default)
        => Task.FromResult(true);
}
