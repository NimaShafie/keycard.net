using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.Users;

namespace KeyCard.Infrastructure.Models.Bookings
{
    public class Booking : IDeletable
    {
        public Guid Id { get; set; }
        public string ConfirmationCode { get; set; } = default!;

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Reserved;

        public int Adults { get; set; }
        public int Children { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPrepaid { get; set; }

        public Guid GuestProfileId { get; set; }
        public GuestProfile GuestProfile { get; set; } = default!;

        public Guid RoomId { get; set; }
        public Room Room { get; set; } = default!;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public Invoice? Invoice { get; set; }
        public DigitalKey? DigitalKey { get; set; }
        public bool IsDeleted { get; set; }

    }

}
