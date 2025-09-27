// Services/MockBookingService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeyCard.Desktop.Models;
namespace KeyCard.Desktop.Services;

public sealed class MockBookingService : IBookingService
{
    private readonly List<Booking> _b = new()
    {
        new("BKG-1001","Alice Johnson",0,DateTime.Today,DateTime.Today.AddDays(2)),
        new("BKG-1002","Bob Smith",0,DateTime.Today,DateTime.Today.AddDays(1)),
        new("BKG-1003","Chen Li",0,DateTime.Today.AddDays(1),DateTime.Today.AddDays(4)),
    };
    public Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync()
        => Task.FromResult<IReadOnlyList<Booking>>(_b.Where(x => x.CheckIn.Date == DateTime.Today).ToList());
    public Task<Booking?> FindBookingByCodeAsync(string q)
        => Task.FromResult(_b.FirstOrDefault(x => x.BookingId.Equals(q, StringComparison.OrdinalIgnoreCase) || x.GuestName.Contains(q, StringComparison.OrdinalIgnoreCase)));
    public Task<bool> AssignRoomAsync(string id, int room)
    { var i = _b.FindIndex(x => x.BookingId == id); if (i < 0) return Task.FromResult(false); _b[i] = _b[i] with { RoomNumber = room }; return Task.FromResult(true); }
    public Task<bool> CheckInAsync(string id) => Task.FromResult(_b.Any(x => x.BookingId == id));
}
