// Services/Live/ApiServices.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Infrastructure.Api;
using KeyCard.Desktop.Services;

using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop.Services.Live
{
    // -------------------------
    // Common API base
    // -------------------------
    internal abstract class ApiBase
    {
        private readonly IHttpClientFactory _factory;
        private readonly IAppEnvironment _env;

        private string? _bearer;

        protected ApiBase(IHttpClientFactory factory, IAppEnvironment env)
        {
            _factory = factory;
            _env = env;
        }

        protected HttpClient Create()
        {
            var c = _factory.CreateClient("Api");
            var baseUrl = _env.ApiBaseUrl?.TrimEnd('/') + "/";
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("ApiBaseUrl is not configured.");
            c.BaseAddress = new Uri(baseUrl);

            if (!string.IsNullOrWhiteSpace(_bearer))
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearer);

            return c;
        }

        protected void SetBearer(string? token) => _bearer = token;

        protected static void EnsureRoute(string? route, string name)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new NotSupportedException($"Backend route '{name}' is not configured. Set Api:Routes:{name} in appsettings.");
        }
    }

    // -------------------------
    // AUTH (low-level client)
    // -------------------------
    internal sealed class AuthApi : ApiBase
    {
        private readonly ApiRoutes _routes;
        private readonly ILogger<AuthApi> _logger;

        public string? AccessToken { get; private set; }
        public DateTimeOffset? AccessTokenExpiresAt { get; private set; }

        public AuthApi(IHttpClientFactory factory, IAppEnvironment env, ApiRoutes routes, ILogger<AuthApi> logger)
            : base(factory, env)
        {
            _routes = routes;
            _logger = logger;
        }

        public async Task<StaffLoginResponse> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            EnsureRoute(_routes.StaffLogin, nameof(_routes.StaffLogin));
            var c = Create();

            var req = new StaffLoginRequest { Username = username, Password = password };
            using var resp = await c.PostAsJsonAsync(_routes.StaffLogin, req, ct);
            resp.EnsureSuccessStatusCode();

            var data = await resp.Content.ReadFromJsonAsync<StaffLoginResponse>(cancellationToken: ct)
                       ?? throw new InvalidOperationException("Empty login response");

            AccessToken = data.AccessToken;
            AccessTokenExpiresAt = data.ExpiresAt;
            SetBearer(AccessToken);

            _logger.LogInformation("Staff login ok; token expires at {When}", data.ExpiresAt);
            return data;
        }

        public void Logout()
        {
            AccessToken = null;
            AccessTokenExpiresAt = null;
            SetBearer(null);
        }
    }

    // -------------------------
    // HOTELS
    // -------------------------
    internal sealed class HotelsApi : ApiBase
    {
        private readonly ApiRoutes _routes;
        public HotelsApi(IHttpClientFactory f, IAppEnvironment e, ApiRoutes r) : base(f, e) { _routes = r; }

        public async Task<IReadOnlyList<HotelDto>> ListAsync(CancellationToken ct = default)
        {
            EnsureRoute(_routes.Hotels_List, nameof(_routes.Hotels_List));
            var c = Create();
            return await c.GetFromJsonAsync<IReadOnlyList<HotelDto>>(_routes.Hotels_List, ct) ?? Array.Empty<HotelDto>();
        }
    }

    // -------------------------
    // ROOMS
    // -------------------------
    internal sealed class RoomsApi : ApiBase
    {
        private readonly ApiRoutes _routes;
        public RoomsApi(IHttpClientFactory f, IAppEnvironment e, ApiRoutes r) : base(f, e) { _routes = r; }

        public async Task<IReadOnlyList<RoomDto>> ListAsync(Guid hotelId, string? status = null, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Rooms_List, nameof(_routes.Rooms_List));
            var c = Create();
            var url = _routes.Rooms_List + $"?hotelId={hotelId}" + (string.IsNullOrWhiteSpace(status) ? "" : $"&status={Uri.EscapeDataString(status)}");
            return await c.GetFromJsonAsync<IReadOnlyList<RoomDto>>(url, ct) ?? Array.Empty<RoomDto>();
        }

        public async Task<RoomDto> GetAsync(Guid roomId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Rooms_Get, nameof(_routes.Rooms_Get));
            var c = Create();
            var path = _routes.Format(_routes.Rooms_Get, ("roomId", roomId.ToString()));
            return await c.GetFromJsonAsync<RoomDto>(path, ct) ?? throw new InvalidOperationException("Room not found");
        }

        public async Task UpdateStatusAsync(Guid roomId, UpdateRoomStatusRequest req, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Rooms_UpdateStatus, nameof(_routes.Rooms_UpdateStatus));
            var c = Create();
            var path = _routes.Format(_routes.Rooms_UpdateStatus, ("roomId", roomId.ToString()));
            var resp = await c.PutAsJsonAsync(path, req, ct);
            resp.EnsureSuccessStatusCode();
        }
    }

    // -------------------------
    // HOUSEKEEPING (admin)
    // -------------------------
    internal sealed class HousekeepingApi : ApiBase
    {
        private readonly ApiRoutes _routes;
        public HousekeepingApi(IHttpClientFactory f, IAppEnvironment e, ApiRoutes r) : base(f, e) { _routes = r; }

        public async Task<IReadOnlyList<HousekeepingTaskDto>> ListTasksAsync(Guid hotelId, string? status = null, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Hk_Tasks_List, nameof(_routes.Hk_Tasks_List));
            var c = Create();

            var url = _routes.Hk_Tasks_List;
            if (hotelId != Guid.Empty || !string.IsNullOrWhiteSpace(status))
            {
                var sep = url.Contains("?") ? "&" : "?";
                url += sep + $"hotelId={hotelId}";
                if (!string.IsNullOrWhiteSpace(status))
                    url += $"&status={Uri.EscapeDataString(status)}";
            }

            return await c.GetFromJsonAsync<IReadOnlyList<HousekeepingTaskDto>>(url, ct) ?? Array.Empty<HousekeepingTaskDto>();
        }

        public async Task AssignTaskAsync(Guid taskId, AssignTaskRequest req, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Hk_Task_Assign, nameof(_routes.Hk_Task_Assign));
            var c = Create();
            var path = _routes.Format(_routes.Hk_Task_Assign, ("taskId", taskId.ToString()));
            var resp = await c.PutAsJsonAsync(path, req, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task CompleteTaskAsync(Guid taskId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Hk_Task_Complete, nameof(_routes.Hk_Task_Complete));
            var c = Create();
            var path = _routes.Format(_routes.Hk_Task_Complete, ("taskId", taskId.ToString()));
            var resp = await c.PostAsync(path, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }
    }

    // -------------------------
    // BOOKINGS
    // -------------------------
    internal sealed class BookingsApi : ApiBase
    {
        private readonly ApiRoutes _routes;
        public BookingsApi(IHttpClientFactory f, IAppEnvironment e, ApiRoutes r) : base(f, e) { _routes = r; }

        public async Task<BookingDto?> GetByConfirmationAsync(string confirmationCode, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Bookings_GetByCode, nameof(_routes.Bookings_GetByCode));
            var c = Create();
            var url = _routes.Bookings_GetByCode;
            var sep = url.Contains("?") ? "&" : "?";
            url += sep + $"code={Uri.EscapeDataString(confirmationCode)}";
            return await c.GetFromJsonAsync<BookingDto>(url, ct);
        }

        public async Task<BookingDto?> GetByIdAsync(int bookingId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Bookings_Get, nameof(_routes.Bookings_Get));
            var c = Create();
            var path = _routes.Format(_routes.Bookings_Get, ("bookingId", bookingId.ToString()));
            return await c.GetFromJsonAsync<BookingDto>(path, ct);
        }

        public async Task CheckInByIdAsync(int bookingId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Bookings_CheckIn, nameof(_routes.Bookings_CheckIn));
            var c = Create();
            var path = _routes.Format(_routes.Bookings_CheckIn, ("bookingId", bookingId.ToString()));
            var resp = await c.PostAsync(path, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task CheckOutByIdAsync(int bookingId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Bookings_CheckOut, nameof(_routes.Bookings_CheckOut));
            var c = Create();
            var path = _routes.Format(_routes.Bookings_CheckOut, ("bookingId", bookingId.ToString()));
            var resp = await c.PostAsync(path, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }
    }

    // -------------------------
    // DIGITAL KEYS
    // -------------------------
    internal sealed class KeysApi : ApiBase
    {
        private readonly ApiRoutes _routes;
        public KeysApi(IHttpClientFactory f, IAppEnvironment e, ApiRoutes r) : base(f, e) { _routes = r; }

        public async Task<DigitalKeyDto> IssueByBookingAsync(Guid bookingId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Keys_Issue, nameof(_routes.Keys_Issue));
            var c = Create();
            var path = _routes.Format(_routes.Keys_Issue, ("bookingId", bookingId.ToString()));
            var resp = await c.PostAsync(path, content: null, ct);
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadFromJsonAsync<DigitalKeyDto>(cancellationToken: ct))!;
        }

        public async Task RevokeByBookingAsync(Guid bookingId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Keys_Revoke, nameof(_routes.Keys_Revoke));
            var c = Create();
            var path = _routes.Format(_routes.Keys_Revoke, ("bookingId", bookingId.ToString()));
            var resp = await c.PostAsync(path, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }
    }

    // -------------------------
    // PAYMENTS
    // -------------------------
    internal sealed class PaymentsApi : ApiBase
    {
        private readonly ApiRoutes _routes;
        public PaymentsApi(IHttpClientFactory f, IAppEnvironment e, ApiRoutes r) : base(f, e) { _routes = r; }

        public async Task<PaymentDto> ChargeAsync(PaymentRequest req, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Payments_Charge, nameof(_routes.Payments_Charge));
            var c = Create();
            var resp = await c.PostAsJsonAsync(_routes.Payments_Charge, req, ct);
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadFromJsonAsync<PaymentDto>(cancellationToken: ct))!;
        }

        public async Task<IReadOnlyList<PaymentDto>> ListByBookingAsync(Guid bookingId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Payments_ByBooking, nameof(_routes.Payments_ByBooking));
            var c = Create();
            var path = _routes.Format(_routes.Payments_ByBooking, ("bookingId", bookingId.ToString()));
            return await c.GetFromJsonAsync<IReadOnlyList<PaymentDto>>(path, ct) ?? Array.Empty<PaymentDto>();
        }
    }

    // -------------------------
    // INVOICES
    // -------------------------
    internal sealed class InvoicesApi : ApiBase
    {
        private readonly ApiRoutes _routes;
        public InvoicesApi(IHttpClientFactory f, IAppEnvironment e, ApiRoutes r) : base(f, e) { _routes = r; }

        public async Task<InvoiceDto?> GetByBookingAsync(Guid bookingId, CancellationToken ct = default)
        {
            EnsureRoute(_routes.Invoices_ByBooking, nameof(_routes.Invoices_ByBooking));
            var c = Create();
            var path = _routes.Format(_routes.Invoices_ByBooking, ("bookingId", bookingId.ToString()));
            return await c.GetFromJsonAsync<InvoiceDto>(path, ct);
        }
    }
}
