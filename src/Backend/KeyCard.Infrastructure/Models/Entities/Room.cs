using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models.Entities
{
    public class Room : IDeletable
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = default!;
        public RoomStatus Status { get; set; } = RoomStatus.Vacant;

        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; } = default!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public bool IsDeleted { get; set; }
    }

}
