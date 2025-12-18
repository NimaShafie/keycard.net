// Services/Api/BookingService.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Generated;
using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services.Api;

public sealed class BookingService : IBookingService
{
    private readonly KeyCardApiClient _client;
    public BookingService(KeyCardApiClient client) => _client = client;

    public async Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct = default)
        => (await _client.BookingsAllAsync(ct)).Select(x => x.ToModel()).ToList();

    public async Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default)
        => (await _client.BookingsAsync(code, ct))?.ToModel();

    public async Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync(CancellationToken ct = default)
    {
        var today = System.DateTime.Today.Date;
        var all = await ListAsync(ct);
        return all.Where(b => b.CheckInDate.Date == today).ToList();
    }

    public Task<Booking?> FindBookingByCodeAsync(string code, CancellationToken ct = default)
        => GetByCodeAsync(code, ct);

    // Stubs until backend endpoints exist
    public Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default) => Task.FromResult(true);
}
