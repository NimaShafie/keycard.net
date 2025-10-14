// Infrastructure/Api/ApiRoutes.cs
using System;

namespace KeyCard.Desktop.Infrastructure.Api
{
    /// <summary>
    /// Config-driven API paths. Defaults match your Backend; override via appsettings.
    /// </summary>
    public sealed class ApiRoutes
    {
        // Auth
        public string StaffLogin { get; set; } = "api/auth/login";

        // Hotels
        public string Hotels_List { get; set; } = "";

        // Rooms
        public string Rooms_List { get; set; } = "";
        public string Rooms_Get { get; set; } = "";
        public string Rooms_UpdateStatus { get; set; } = "";

        // Housekeeping
        public string Hk_Tasks_List { get; set; } = "api/admin/housekeeping";
        public string Hk_Task_Assign { get; set; } = "api/admin/housekeeping/{taskId}";
        public string Hk_Task_Complete { get; set; } = "api/admin/housekeeping/{taskId}/complete";

        // Bookings
        public string Bookings_Get { get; set; } = "api/admin/bookings/{bookingId}";
        public string Bookings_GetByCode { get; set; } = "api/guest/bookings/lookup";
        public string Bookings_CheckIn { get; set; } = "api/admin/bookings/{bookingId}/checkin";
        public string Bookings_CheckOut { get; set; } = "api/admin/bookings/{bookingId}/checkout";

        // Digital Keys
        public string Keys_Issue { get; set; } = "api/admin/digitalkey/{bookingId}/key";
        public string Keys_Revoke { get; set; } = "api/admin/digitalkey/{bookingId}/key/revoke";

        // Payments / Invoices
        public string Payments_Charge { get; set; } = "";
        public string Payments_ByBooking { get; set; } = "";
        public string Invoices_ByBooking { get; set; } = "";

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1822:Mark members as static",
    Justification = "Intentional instance method on a DI-bound settings object")]

        public string Format(string template, params (string token, string value)[] pairs)
        {
            foreach (var (token, value) in pairs)
                template = template.Replace("{" + token + "}", Uri.EscapeDataString(value));
            return template;
        }
    }
}
