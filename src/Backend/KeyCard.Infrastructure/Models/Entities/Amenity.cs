using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;

namespace KeyCard.Infrastructure.Models.Entities
{
    public class Amenity : IAuditable, IDeletable
    {
        public int Id { get; set; }
        public string Key { get; set; } = null!;      // e.g., "wifi", "tv", "coffee"
        public string Label { get; set; } = null!;    // e.g., "Free WiFi"
        public string? Description { get; set; }
        public string? IconKey { get; set; }          // UI hint (optional)

        public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
