// ============================================================================
// DIGITAL KEY MODEL - MOBILE ROOM ACCESS
// this is the modern replacement for plastic key cards!
// guest shows this on phone to unlock room door
// much cooler and more secure than magnetic stripe cards
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models
{
    /// <summary>
    /// Digital key for mobile room access
    /// Contains a cryptographic token that unlocks the room door
    /// Guest shows QR code or uses NFC with this token
    /// </summary>
    public class DigitalKey : IDeletable, IAuditable
    {
        public int Id { get; set; }
        
        /// <summary>
        /// The secret token - 256 bits of random data encoded as Base64
        /// Door lock verifies this token to grant access
        /// Keep this secure! Anyone with token can open the door
        /// </summary>
        public string Token { get; set; } = default!;
        
        /// <summary>
        /// When the key was created - usually at check-in time
        /// </summary>
        public DateTime IssuedAt { get; set; }
        
        /// <summary>
        /// When the key expires - usually checkout date
        /// After this, token wont work even if not revoked
        /// Automatic security - no zombie keys floating around
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Has this key been manually revoked?
        /// Set to true at checkout or if key is compromised
        /// Once revoked, door lock rejects this token
        /// </summary>
        public bool IsRevoked { get; set; }

        // link to the booking this key belongs to
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        // ===== Audit trail =====
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
