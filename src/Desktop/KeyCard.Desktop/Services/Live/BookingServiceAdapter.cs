// Services/Live/BookingServiceAdapter.cs
#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services.Live
{
    public sealed class BookingServiceAdapter : IBookingService
    {
        public BookingServiceAdapter() { }

        public Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct = default)
        {
            IReadOnlyList<Booking> empty = System.Array.Empty<Booking>();
            return Task.FromResult(empty);
        }

        public Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            return Task.FromResult<Booking?>(null);
        }

        public Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync(CancellationToken ct = default)
        {
            IReadOnlyList<Booking> empty = System.Array.Empty<Booking>();
            return Task.FromResult(empty);
        }

        public Task<Booking?> FindBookingByCodeAsync(string code, CancellationToken ct = default)
        {
            return Task.FromResult<Booking?>(null);
        }

        public Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }
    }
}
