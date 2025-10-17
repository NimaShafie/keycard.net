using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;

namespace KeyCard.Infrastructure.Models.Entities
{
    public class RoomTypeAmenity : IAuditable, IDeletable
    {
        public int Id { get; set; }
        public int RoomTypeId { get; set; }
        public int AmenityId { get; set; }

        public string? ValueText { get; set; }
        public int? ValueInt { get; set; }
        public decimal? ValueDecimal { get; set; }

        public RoomType RoomType { get; set; } = null!;
        public Amenity Amenity { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
