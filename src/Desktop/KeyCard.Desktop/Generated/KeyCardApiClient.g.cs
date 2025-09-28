// Generated/KeyCardApiClient.g.cs

#nullable enable
// TEMPORARY STUB – replace with NSwag output later.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCard.Desktop.Generated
{
    public class BookingDto
    {
        public Guid Id { get; set; }
        public string ConfirmationCode { get; set; } = "";
        public string GuestLastName { get; set; } = "";
        public int RoomNumber { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public string Status { get; set; } = "Reserved";
    }

    public class KeyCardApiClient
    {
        private readonly HttpClient _http;
        public KeyCardApiClient(HttpClient http) => _http = http;

        public Task<IEnumerable<BookingDto>> BookingsAllAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<BookingDto>>(Array.Empty<BookingDto>());

        public Task<BookingDto?> BookingsAsync(string code, CancellationToken ct = default)
            => Task.FromResult<BookingDto?>(null);
    }
}
