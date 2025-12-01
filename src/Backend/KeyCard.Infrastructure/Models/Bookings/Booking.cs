// ============================================================================
// BOOKING MODEL - THE CORE OF HOTEL BUSINESS
// a booking represents a guest's reservation for a room
// this is probably the most important entity in the whole system!
// ============================================================================

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.User;

namespace KeyCard.Infrastructure.Models.Bookings
{
    /// <summary>
    /// Booking entity - represents a hotel room reservation
    /// Tracks the entire guest journey: Reserved → CheckedIn → CheckedOut
    /// Links guest to room for specific dates
    /// </summary>
    public class Booking : IDeletable, IAuditable
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Unique code like "KCN-123456" - guest uses this to look up booking
        /// Printed on confirmation email, used at kiosk check-in
        /// </summary>
        public string ConfirmationCode { get; set; } = default!;

        // ===== Stay dates =====
        public DateTime CheckInDate { get; set; }   // when guest is supposed to arrive
        public DateTime CheckOutDate { get; set; }  // when guest is supposed to leave
        
        /// <summary>
        /// Booking lifecycle: Reserved → CheckedIn → CheckedOut (or Cancelled)
        /// </summary>
        public BookingStatus Status { get; set; } = BookingStatus.Reserved;

        // actual times when guest checked in/out (null until it happens)
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        // ===== Guest info =====
        public int Adults { get; set; }     // number of adult guests
        public int Children { get; set; }   // number of children (for capacity planning)
        
        // ===== Money stuff =====
        public decimal TotalAmount { get; set; }  // room charge total
        public decimal ExtraFees { get; set; } = 0m;  // minibar, room service, damage, etc.
        public bool IsPrepaid { get; set; }  // did they pay upfront or pay at checkout?

        // ===== Relationships =====
        public int GuestProfileId { get; set; }
        public ApplicationUser GuestProfile { get; set; } = default!;  // the guest who booked

        public int RoomId { get; set; }
        public Room Room { get; set; } = default!;  // which room they're staying in

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();  // payment records
        public Invoice? Invoice { get; set; }  // generated at checkout
        public DigitalKey? DigitalKey { get; set; }  // mobile room key

        // ===== Audit trail =====
        // we track who created/modified every record - important for accountability!
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }  // soft delete - never actually remove data

        /// <summary>
        /// Change booking status - uses method to allow for future validation/events
        /// </summary>
        public void ChangeStatus(BookingStatus status)
        {
            // TODO: could add validation here (e.g., cant go from CheckedOut back to Reserved)
            this.Status = status;
        }
    }
}
