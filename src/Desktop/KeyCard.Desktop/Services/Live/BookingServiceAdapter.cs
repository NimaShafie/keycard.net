// Services/Live/BookingServiceAdapter.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services.Live
{
    /// <summary>
    /// Wrapper for API responses that contain Result array
    /// </summary>
    internal sealed class ApiResponse<T>
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("result")]
        public T? Result { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }

    public sealed class BookingServiceAdapter : IBookingService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BookingServiceAdapter(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await Client.GetFromJsonAsync<ApiResponse<List<Booking>>>(
                    "api/admin/Bookings/GetAllBookings",
                    cancellationToken: ct);

                return response?.Result ?? new List<Booking>();
            }
            catch
            {
                return Array.Empty<Booking>();
            }
        }

        public async Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            try
            {
                var response = await Client.GetFromJsonAsync<ApiResponse<List<Booking>>>(
                    $"api/admin/Bookings/GetAllBookings?guestName={Uri.EscapeDataString(code)}",
                    cancellationToken: ct);

                var bookings = response?.Result;
                return bookings?.Count > 0 ? bookings[0] : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync(CancellationToken ct = default)
        {
            try
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var response = await Client.GetFromJsonAsync<ApiResponse<List<Booking>>>(
                    $"api/admin/Bookings/GetAllBookings?fromDate={today}&toDate={today}",
                    cancellationToken: ct);

                return response?.Result ?? new List<Booking>();
            }
            catch
            {
                return Array.Empty<Booking>();
            }
        }

        public Task<Booking?> FindBookingByCodeAsync(string code, CancellationToken ct = default)
            => GetByCodeAsync(code, ct);

        public async Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default)
        {
            try
            {
                using var resp = await Client.PostAsJsonAsync(
                    $"api/admin/bookings/code/{Uri.EscapeDataString(bookingCode)}/assign-room",
                    new { roomNumber },
                    ct);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default)
        {
            try
            {
                // bookingCode here should be the booking ID
                using var resp = await Client.PostAsync(
                    $"api/admin/Bookings/{bookingCode}/checkin",
                    null,
                    ct);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
