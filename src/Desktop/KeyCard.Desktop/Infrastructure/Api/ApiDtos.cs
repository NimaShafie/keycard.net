// Infrastructure/Api/ApiDtos.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KeyCard.Desktop.Infrastructure.Api
{
    // Keep names aligned with Backend’s JSON (Pascal or camel—System.Text.Json handles both)

    public enum RoomStatusDto
    {
        Unknown = 0,
        Vacant = 1,
        Occupied = 2,
        Dirty = 3,
        Cleaning = 4,
        OutOfService = 5,
        Ready = 6
    }

    public sealed class HotelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public string Country { get; set; } = "";
    }

    public sealed class RoomTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Capacity { get; set; }
        public decimal BaseRate { get; set; }
        public decimal SeasonalRate { get; set; }
        public Guid HotelId { get; set; }
    }

    public sealed class RoomDto
    {
        public Guid Id { get; set; }
        public string RoomNumber { get; set; } = "";
        public int Floor { get; set; }
        public RoomStatusDto Status { get; set; }
        public Guid RoomTypeId { get; set; }
        public Guid HotelId { get; set; }
        public RoomTypeDto? RoomType { get; set; }
    }

    public sealed class GuestProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Country { get; set; } = "";
    }

    public sealed class BookingDto
    {
        public Guid Id { get; set; }
        public string ConfirmationCode { get; set; } = "";
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
        public Guid GuestProfileId { get; set; }
        public Guid RoomId { get; set; }
        public GuestProfileDto? Guest { get; set; }
        public RoomDto? Room { get; set; }
    }

    public sealed class HousekeepingTaskDto
    {
        public Guid Id { get; set; }
        public string TaskName { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Status { get; set; } = ""; // e.g., Pending/InProgress/Done
        public DateTimeOffset? CompletedAt { get; set; }
        public Guid RoomId { get; set; }
        public Guid? AssignedTo { get; set; } // Staff UserId
    }

    public sealed class InvoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public DateTimeOffset IssuedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public Guid BookingId { get; set; }
        public string? PdfPath { get; set; }
    }

    public sealed class PaymentDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public DateTimeOffset PaidAt { get; set; }
        public string Method { get; set; } = ""; // Card/Cash/etc
        public string TransactionId { get; set; } = "";
        public Guid BookingId { get; set; }
    }

    public sealed class DigitalKeyDto
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = "";
        public DateTimeOffset IssuedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public Guid BookingId { get; set; }
    }

    // Requests

    public sealed class StaffLoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class StaffLoginResponse
    {
        public string AccessToken { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
    }

    public sealed class UpdateRoomStatusRequest
    {
        public RoomStatusDto Status { get; set; }
    }

    public sealed class AssignTaskRequest
    {
        public Guid? StaffUserId { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class CheckInRequest
    {
        public string ConfirmationCode { get; set; } = "";
        public Guid RoomId { get; set; }
        public DateTimeOffset? At { get; set; }
    }

    public sealed class CheckOutRequest
    {
        public Guid BookingId { get; set; }
        public DateTimeOffset? At { get; set; }
    }

    public sealed class IssueDigitalKeyRequest
    {
        public Guid BookingId { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public sealed class PaymentRequest
    {
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = "Card";
    }
}
