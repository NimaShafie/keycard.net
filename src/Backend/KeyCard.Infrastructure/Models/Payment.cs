using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string Method { get; set; } = default!; // Card, Cash, Online

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = default!;
    }

}
