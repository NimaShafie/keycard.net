using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;

namespace KeyCard.Infrastructure.Models.Entities
{
    public class RoomType : IDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!; // e.g., Deluxe, Suite
        public decimal BaseRate { get; set; }
        public int Capacity { get; set; }

        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } = default!;
        public bool IsDeleted { get; set; }

    }

}
