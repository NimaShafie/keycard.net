using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models
{
    public class Payment : IDeletable, IAuditable
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string Method { get; set; } = default!; // Card, Cash, Online
        public string? TransactionId { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }

}
