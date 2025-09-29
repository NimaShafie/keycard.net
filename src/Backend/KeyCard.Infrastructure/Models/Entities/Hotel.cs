using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;

namespace KeyCard.Infrastructure.Models.Entities
{
    public class Hotel : IDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Address { get; set; } = default!;

        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public bool IsDeleted { get; set; }

    }

}
