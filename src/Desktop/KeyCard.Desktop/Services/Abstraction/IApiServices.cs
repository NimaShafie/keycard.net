// Services/Abstraction/IApiServices.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Infrastructure.Api;

namespace KeyCard.Desktop.Services.Abstractions
{
    public interface IAuthService
    {
        Task<StaffLoginResponse> LoginAsync(StaffLoginRequest req, CancellationToken ct = default);
        void SetAccessToken(string? token);
        string? GetAccessToken();
    }

    public interface IRoomsService
    {
        Task<IReadOnlyList<RoomDto>> ListAsync(Guid hotelId, string? status = null, CancellationToken ct = default);
        Task<RoomDto> GetAsync(Guid roomId, CancellationToken ct = default);
        Task UpdateStatusAsync(Guid roomId, UpdateRoomStatusRequest req, CancellationToken ct = default);
    }

    public interface IHousekeepingService
    {
        Task<IReadOnlyList<HousekeepingTaskDto>> ListTasksAsync(Guid hotelId, string? status = null, CancellationToken ct = default);
        Task AssignTaskAsync(Guid taskId, AssignTaskRequest req, CancellationToken ct = default);
        Task CompleteTaskAsync(Guid taskId, CancellationToken ct = default);
    }

    public interface IBookingsService
    {
        Task<BookingDto?> GetByConfirmationAsync(string confirmationCode, CancellationToken ct = default);
        Task<BookingDto?> GetAsync(Guid bookingId, CancellationToken ct = default);
        Task CheckInAsync(CheckInRequest req, CancellationToken ct = default);
        Task CheckOutAsync(CheckOutRequest req, CancellationToken ct = default);
    }

    public interface IKeysService
    {
        Task<DigitalKeyDto> IssueAsync(IssueDigitalKeyRequest req, CancellationToken ct = default);
        Task RevokeAsync(Guid digitalKeyId, CancellationToken ct = default);
    }

    public interface IPaymentsService
    {
        Task<PaymentDto> ChargeAsync(PaymentRequest req, CancellationToken ct = default);
        Task<IReadOnlyList<PaymentDto>> ListByBookingAsync(Guid bookingId, CancellationToken ct = default);
    }

    public interface IInvoicesService
    {
        Task<InvoiceDto?> GetByBookingAsync(Guid bookingId, CancellationToken ct = default);
        // Optionally: Task<byte[]> DownloadPdfAsync(Guid invoiceId, CancellationToken ct = default);
    }

    public interface IHotelsService
    {
        Task<IReadOnlyList<HotelDto>> ListAsync(CancellationToken ct = default);
    }
}
