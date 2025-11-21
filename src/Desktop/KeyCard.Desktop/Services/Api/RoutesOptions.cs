// Services/Api/RoutesOptions.cs
using System;

namespace KeyCard.Desktop.Services.Api
{
    /// <summary>
    /// Strongly-typed view over "Api:Routes" from configuration.
    /// Used by LIVE services (e.g., BookingService) to resolve endpoint paths.
    /// </summary>
    public sealed class RoutesOptions
    {
        public string? StaffLogin { get; set; }
        public string? StaffRegister { get; set; }
        public string? Bookings_Get { get; set; }
        public string? Bookings_GetByCode { get; set; }
        public string? Bookings_CheckIn { get; set; }
        public string? Bookings_CheckOut { get; set; }
        public string? Hk_Tasks_List { get; set; }
        public string? Hk_Task_Assign { get; set; }
        public string? Hk_Task_Complete { get; set; }
        public string? Keys_Issue { get; set; }
        public string? Keys_Revoke { get; set; }
        public string? Hotels_List { get; set; }
        public string? Rooms_List { get; set; }
        public string? Rooms_Get { get; set; }
        public string? Rooms_UpdateStatus { get; set; }
        public string? Payments_Charge { get; set; }
        public string? Payments_ByBooking { get; set; }
        public string? Invoices_ByBooking { get; set; }
    }
}
