using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;

namespace KeyCard.Infrastructure.Models.Entities
{
    public class Hotel : IAuditable, IDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string City { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string ContactEmail { get; set; } = default!;
        public string ContactPhone { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }

        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public bool IsDeleted { get; set; }

    }

}
