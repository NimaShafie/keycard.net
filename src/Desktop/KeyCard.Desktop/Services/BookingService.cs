// Services/BookingService.cs
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services;

/// <summary>
/// LIVE implementation backed by your HTTP API. It uses the named HttpClient "Api"
/// configured in ServiceRegistration.AddKeyCardDesktopServices().
/// Routes are read from ApiRoutesOptions (bound from "Api:Routes").
/// </summary>
public sealed class BookingService : IBookingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BookingService> _logger;
    private readonly ApiRoutesOptions _routes;

    public BookingService(
        IHttpClientFactory httpClientFactory,
        IOptions<ApiRoutesOptions> routes,
        ILogger<BookingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _routes = routes.Value;
    }

    private HttpClient Client => _httpClientFactory.CreateClient("Api");

    public async Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            var list = await Client.GetFromJsonAsync<List<Booking>>("api/admin/bookings", cancellationToken: ct);
            return (IReadOnlyList<Booking>)(list ?? new List<Booking>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ListAsync failed; returning empty list.");
            return Array.Empty<Booking>();
        }
    }

    public async Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var route = _routes.Bookings_GetByCode ?? "api/guest/bookings/lookup";
        try
        {
            var payload = new { code };
            using var resp = await Client.PostAsJsonAsync(route, payload, ct);
            if (!resp.IsSuccessStatusCode) return null;

            var booking = await resp.Content.ReadFromJsonAsync<Booking>(cancellationToken: ct);
            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetByCodeAsync failed for code {Code}.", code);
            return null;
        }
    }

    public async Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync(CancellationToken ct = default)
    {
        try
        {
            var list = await Client.GetFromJsonAsync<List<Booking>>("api/admin/bookings/today-arrivals", cancellationToken: ct);
            return (IReadOnlyList<Booking>)(list ?? new List<Booking>());
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "GetTodayArrivalsAsync not implemented on API; returning empty.");
            return Array.Empty<Booking>();
        }
    }

    public Task<Booking?> FindBookingByCodeAsync(string code, CancellationToken ct = default)
        => GetByCodeAsync(code, ct);

    public async Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default)
    {
        try
        {
            var route = $"api/admin/bookings/code/{Uri.EscapeDataString(bookingCode)}/assign-room";
            using var resp = await Client.PostAsJsonAsync(route, new { roomNumber }, ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AssignRoomAsync failed for {Code}.", bookingCode);
            return false;
        }
    }

    public async Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default)
    {
        try
        {
            var routeTemplate = _routes.Bookings_CheckIn ?? "api/admin/bookings/{bookingId}/checkin";
            var route = routeTemplate.Contains("{bookingId}", StringComparison.OrdinalIgnoreCase)
                ? routeTemplate.Replace("{bookingId}", Uri.EscapeDataString(bookingCode))
                : $"api/admin/bookings/code/{Uri.EscapeDataString(bookingCode)}/checkin";

            using var resp = await Client.PostAsync(route, content: null, ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CheckInAsync failed for {Code}.", bookingCode);
            return false;
        }
    }
}
