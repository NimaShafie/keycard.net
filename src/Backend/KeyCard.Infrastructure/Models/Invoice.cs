using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models
{
    public class Invoice : IDeletable, IAuditable
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public DateTime IssuedAt { get; set; }
        public string PdfPath { get; set; } = default!;
        public decimal TotalAmount { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }

}
