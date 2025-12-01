// ============================================================================
// DIGITAL KEY SERVICE - MOBILE ROOM ACCESS MAGIC!
// this is the cool modern feature that replaces plastic key cards
// guest uses phone to unlock room door - QR code or NFC
// very secure with cryptographic tokens
// ============================================================================

using System.Security.Cryptography;

using KeyCard.BusinessLogic.Commands.Admin.DigitalKey;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models;
using KeyCard.Infrastructure.Models.AppDbContext;

using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    /// <summary>
    /// Digital key service - manages mobile room access tokens
    /// Each booking gets a unique cryptographic token at check-in
    /// Token is verified by door lock system (or shown as QR at door)
    /// </summary>
    public class DigitalKeyService : IDigitalKeyService
    {
        private readonly ApplicationDBContext _context;

        public DigitalKeyService(ApplicationDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the digital key for a booking
        /// Guest app calls this to display the key on screen
        /// </summary>
        public async Task<DigitalKeyViewModel> GetDigitalKeyByBookingIdAsync(GetDigitalKeyByBookingIdCommand command, CancellationToken cancellationToken)
        {
            var key = await _context.DigitalKeys
                .FirstOrDefaultAsync(k => k.BookingId == command.BookingId && !k.IsDeleted, cancellationToken);

            if (key == null)
            {
                // maybe not checked in yet?
                throw new KeyNotFoundException("Digital key not found for the specified booking.");
            }

            return new DigitalKeyViewModel()
            {
                Token = key.Token,
                IssuedAt = key.IssuedAt,
                ExpiresAt = key.ExpiresAt,
                BookingId = key.BookingId
            };
        }

        /// <summary>
        /// Issue a new digital key - the magic moment!
        /// Creates a cryptographically secure token that opens the room door
        /// Token expires at checkout - automatic security!
        /// </summary>
        public async Task<DigitalKeyViewModel> IssueKeyAsync(IssueDigitalKeyCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new InvalidOperationException("Booking not found.");

            // only issue key if guest is actually checked in
            // dont want to give key before they arrive!
            if (booking.Status != BookingStatus.CheckedIn)
                throw new InvalidOperationException("Can only issue key for checked-in bookings.");

            // ===== Generate secure random token =====
            // 32 bytes = 256 bits of randomness - very hard to guess!
            // converted to Base64 for easy transmission
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var key = new DigitalKey
            {
                Token = token,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = booking.CheckOutDate,  // auto-expires at checkout!
                BookingId = booking.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.User!.UserId
            };

            _context.DigitalKeys.Add(key);
            await _context.SaveChangesAsync(cancellationToken);

            // return the key info - guest app shows this
            return new DigitalKeyViewModel()
            {
                Id = key.Id,
                Token = key.Token,
                IssuedAt = key.IssuedAt,
                ExpiresAt = key.ExpiresAt,
                IsRevoked = key.IsRevoked,
                BookingId = key.BookingId
            };
        }

        /// <summary>
        /// Revoke a digital key - security first!
        /// Called at checkout or if key is compromised
        /// Once revoked, the token no longer opens the door
        /// </summary>
        public async Task<bool> RevokeKeyAsync(RevokeDigitalKeyCommand command, CancellationToken cancellationToken)
        {
            // find the active (non-revoked) key for this booking
            var key = await _context.DigitalKeys
                .FirstOrDefaultAsync(k => k.BookingId == command.BookingId && !k.IsRevoked, cancellationToken);

            if (key == null) return false;  // no active key to revoke

            // mark as revoked - door lock will reject this token now
            key.IsRevoked = true;
            key.LastUpdatedAt = DateTime.UtcNow;
            key.LastUpdatedBy = command.User!.UserId;

            _context.DigitalKeys.Update(key);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
