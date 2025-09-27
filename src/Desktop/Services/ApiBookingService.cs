// Services/ApiBookingService.cs
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KeyCard.Desktop.Models;
namespace KeyCard.Desktop.Services;

public sealed class ApiBookingService : IBookingService
{
    private readonly HttpClient _http;
    public ApiBookingService(HttpClient http) => _http = http;
    public Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync()
        => _http.GetFromJsonAsync<IReadOnlyList<Booking>>("/api/bookings/today-arrivals")!;
    public Task<Booking?> FindBookingByCodeAsync(string q)
        => _http.GetFromJsonAsync<Booking>($"/api/bookings/find?query={q}");
    public async Task<bool> AssignRoomAsync(string id, int room)
        => (await _http.PostAsJsonAsync("/api/bookings/assign-room", new { bookingId = id, roomNumber = room })).IsSuccessStatusCode;
    public async Task<bool> CheckInAsync(string id)
        => (await _http.PostAsJsonAsync("/api/bookings/check-in", new { bookingId = id })).IsSuccessStatusCode;
}
