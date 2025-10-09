using System.Security.Cryptography;

using KeyCard.BusinessLogic.Commands.DigitalKey;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models;
using KeyCard.Infrastructure.Models.AppDbContext;


using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    public class DigitalKeyService : IDigitalKeyService
    {
        private readonly ApplicationDBContext _context;

        public DigitalKeyService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<DigitalKeyViewModel> IssueKeyAsync(IssueDigitalKeyCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new InvalidOperationException("Booking not found.");

            if (booking.Status != BookingStatus.CheckedIn)
                throw new InvalidOperationException("Can only issue key for checked-in bookings.");

            // Generate a secure random token (HMAC or GUID-based)
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var key = new DigitalKey
            {
                Token = token,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = booking.CheckOutDate,
                BookingId = booking.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.User!.UserId
            };

            _context.DigitalKeys.Add(key);
            await _context.SaveChangesAsync(cancellationToken);

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

        public async Task<bool> RevokeKeyAsync(RevokeDigitalKeyCommand command, CancellationToken cancellationToken)
        {
            var key = await _context.DigitalKeys
                .FirstOrDefaultAsync(k => k.BookingId == command.BookingId && !k.IsRevoked, cancellationToken);

            if (key == null) return false;

            key.IsRevoked = true;
            key.LastUpdatedAt = DateTime.UtcNow;
            key.LastUpdatedBy = command.User!.UserId;

            _context.DigitalKeys.Update(key);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
