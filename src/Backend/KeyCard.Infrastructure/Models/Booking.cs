using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace KeyCard.Infrastructure.Models
{

    //sample table for hotel bookings
    [Table("Blogs")]
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int GuestId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public DateTime CheckIn { get; set; }

        [Required]
        public DateTime CheckOut { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int UpdatedBy { get; set; }
    }
}
