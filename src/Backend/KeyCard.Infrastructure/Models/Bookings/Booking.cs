using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.User;

namespace KeyCard.Infrastructure.Models.Bookings
{
    public class Booking : IDeletable, IAuditable
    {
        public int Id { get; set; }
        public string ConfirmationCode { get; set; } = default!;

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Reserved;

        public int Adults { get; set; }
        public int Children { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPrepaid { get; set; }

        public int GuestProfileId { get; set; }
        public ApplicationUser GuestProfile { get; set; } = default!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = default!;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public Invoice? Invoice { get; set; }
        public DigitalKey? DigitalKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public void ChangeStatus(BookingStatus status)
        {
            this.Status = status;
        }

    }

}
