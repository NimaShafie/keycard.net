using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models
{
    public class DigitalKey : IDeletable, IAuditable
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = default!;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }

        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }

}
