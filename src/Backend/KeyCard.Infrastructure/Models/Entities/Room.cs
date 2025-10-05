using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models.Entities
{
    public class Room : IDeletable, IAuditable
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = default!;
        public string Floor { get; set; } = default!;
        public RoomStatus Status { get; set; } = RoomStatus.Vacant;

        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; } = default!;

        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } = default!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }

}
