using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.Users;

namespace KeyCard.Infrastructure.Models.Bookings
{
    public class Booking : IDeletable
    {
        public int Id { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Reserved;

        public int GuestProfileId { get; set; }
        public GuestProfile GuestProfile { get; set; } = default!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = default!;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public Invoice? Invoice { get; set; }
        public DigitalKey? DigitalKey { get; set; }
        public bool IsDeleted { get; set; }

    }

}
