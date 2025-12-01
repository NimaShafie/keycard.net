// ============================================================================
// BOOKING SERVICE - THE HEART OF HOTEL OPERATIONS
// this is where all the booking magic happens!
// reservations, check-ins, check-outs, cancellations - everything is here
// probably the most important service in the whole system
// ============================================================================

using KeyCard.BusinessLogic.Commands.Admin.Bookings;
using KeyCard.BusinessLogic.Commands.Admin.DigitalKey;
using KeyCard.BusinessLogic.Commands.Guest.Bookings;
using KeyCard.BusinessLogic.Commands.Guest.Invoice;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.AppDbContext;
using KeyCard.Infrastructure.Models.Bookings;
using KeyCard.Infrastructure.Models.HouseKeeping;

using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    /// <summary>
    /// Booking service - handles the entire guest journey
    /// From reservation → check-in → stay → check-out
    /// Coordinates with other services (digital key, invoice, housekeeping)
    /// </summary>
    public class BookingService : IBookingService
    {
        private readonly ApplicationDBContext _context;
        private readonly IDigitalKeyService _digitalKeyService;  // issues room keys
        private readonly IInvoiceService _invoiceServce;  // generates bills

        public BookingService(ApplicationDBContext context, IDigitalKeyService digitalKeyService, IInvoiceService invoiceServce)
        {
            _context = context;
            _digitalKeyService = digitalKeyService;
            _invoiceServce = invoiceServce;
        }

        // ==================== ADMIN OPERATIONS ====================
        // these are used by front desk staff through desktop app
        
        #region Admin
        
        /// <summary>
        /// Create new booking - the moment when reservation becomes real!
        /// Checks if room available, calculates price, generates confirmation code
        /// </summary>
        public async Task<BookingViewModel> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken)
        {
            // IMPORTANT: check if room is actually available for these dates
            // we look for any overlapping bookings that arent cancelled
            bool isAvailable = !await _context.Bookings.AnyAsync(
                b => b.RoomId == command.RoomId &&
                     b.Status != BookingStatus.Cancelled &&
                     b.CheckInDate < command.CheckOutDate &&  // overlap logic
                     b.CheckOutDate > command.CheckInDate &&
                     !b.IsDeleted,
                cancellationToken);

            if (!isAvailable)
                throw new InvalidOperationException("Room is not available for the selected dates.");

            // calculate number of nights (minimum 1, even for same-day checkout)
            var nights = Math.Max(1, (command.CheckOutDate.Date - command.CheckInDate.Date).Days);

            // create the booking with all the details
            var booking = new Booking
            {
                // generate unique confirmation code - guest uses this to look up booking
                // format: KCN-XXXXXX (KeyCard Number - 6 digits)
                ConfirmationCode = $"KCN-{Random.Shared.Next(100000, 999999)}",
                GuestProfileId = command.GuestProfileId,
                RoomId = command.RoomId,
                CheckInDate = command.CheckInDate,
                CheckOutDate = command.CheckOutDate,
                Adults = command.Adults,
                Children = command.Children,
                IsPrepaid = command.IsPrepaid,
                Status = BookingStatus.Reserved,  // starts as reserved, becomes CheckedIn later
                // calculate total: base rate × number of nights
                TotalAmount = await _context.Rooms
                    .Where(r => r.Id == command.RoomId)
                    .Select(r => r.RoomType.BaseRate)
                    .FirstAsync(cancellationToken) * nights,
                // audit trail - who created this and when
                CreatedBy = command.User!.UserId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedBy = command.User!.UserId
            };

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync(cancellationToken);

            // fetch related data to return complete view model
            var room = await _context.Rooms.Include(r => r.RoomType)
                .FirstAsync(r => r.Id == command.RoomId, cancellationToken);

            var guest = await _context.Users.FirstAsync(g => g.Id == command.GuestProfileId, cancellationToken);

            return new BookingViewModel(
                booking.Id,
                booking.ConfirmationCode,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.Status,
                guest.FullName,
                room.RoomNumber,
                booking.TotalAmount,
                null  // no digital key yet - issued at check-in
            );
        }

        /// <summary>
        /// Get single booking by ID - staff clicks on a booking to see details
        /// </summary>
        public async Task<BookingViewModel> GetBookingByIdAsync(GetBookingByIdCommand command, CancellationToken cancellationToken)
        {
            // include related entities so we have all info in one query
            var booking = await _context.Bookings
                .Include(b => b.GuestProfile)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Where(b => b.Id == command.BookingId)
                .Select(b => new BookingViewModel(
                    b.Id,
                    b.ConfirmationCode,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.Status,
                    b.GuestProfile.FullName,
                    b.Room.RoomNumber,
                    b.TotalAmount,
                    null
                ))
                .FirstOrDefaultAsync(cancellationToken);

            // booking doesnt exist or was deleted
            if (booking == null)
                throw new KeyNotFoundException("Booking not found.");

            return booking;
        }

        /// <summary>
        /// Get all bookings with filters - powers the booking dashboard
        /// Staff can filter by date range, status, guest name
        /// </summary>
        public async Task<List<BookingViewModel>> GetAllBookingsAsync(GetAllBookingsCommand command, CancellationToken cancellationToken)
        {
            // start with all non-deleted bookings
            var bookingsQuery = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.GuestProfile)
                .Where(b => !b.IsDeleted)
                .AsQueryable();

            // apply filters if provided
            // "show me bookings arriving after March 1st"
            if (command.FromDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.CheckInDate >= command.FromDate.Value);

            // "show me bookings arriving before March 31st"
            if (command.ToDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.CheckInDate <= command.ToDate.Value);

            // "show me only checked-in guests"
            if (!string.IsNullOrWhiteSpace(command.Status) &&
                Enum.TryParse<BookingStatus>(command.Status, true, out var status))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status == status);
            }

            // "find booking for Mr. Johnson"
            if (!string.IsNullOrWhiteSpace(command.GuestName))
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.GuestProfile.FullName.ToLower().Contains(command.GuestName.ToLower()));
            }

            // newest bookings first - staff usually wants to see recent ones
            var list = await bookingsQuery
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingViewModel(
                    b.Id,
                    b.ConfirmationCode,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.Status,
                    b.GuestProfile.FullName,
                    b.Room.RoomNumber,
                    b.TotalAmount,
                    null
                ))
                .ToListAsync(cancellationToken);

            return list;
        }

        /// <summary>
        /// Cancel a booking - guest changed mind or found double booking
        /// Only works if not already checked in!
        /// Also frees up the room for other guests
        /// </summary>
        public async Task<bool> CancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                return false;  // nothing to cancel

            // cant cancel if guest already in the room or already left
            // they would need to do checkout instead
            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut)
                throw new InvalidOperationException("Cannot cancel a booking that has already been checked in or checked out.");

            // mark as cancelled
            booking.ChangeStatus(BookingStatus.Cancelled);
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId;
            _context.Bookings.Update(booking);

            // check if this was the only booking for this room
            // if so, mark room as vacant so it can be booked by others
            bool hasOtherActive = await _context.Bookings.AnyAsync(b =>
                b.RoomId == booking.RoomId &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.CheckedOut &&
                b.Id != booking.Id, cancellationToken);

            if (!hasOtherActive)
            {
                // room is free now!
                booking.Room.ChangeStatus(RoomStatus.Vacant);
                _context.Rooms.Update(booking.Room);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <summary>
        /// Check-in a guest - THE EXCITING MOMENT!
        /// Guest has arrived, time to give them their room
        /// Issues digital key automatically - no more plastic cards!
        /// </summary>
        public async Task<bool> CheckInBookingAsync(CheckInBookingCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new InvalidOperationException("Booking not found.");

            // can only check in a reservation - not a cancelled or already checked-in booking
            if (booking.Status != BookingStatus.Reserved)
                throw new InvalidOperationException("Only reserved bookings can be checked in.");

            // ===== Update booking =====
            booking.ChangeStatus(BookingStatus.CheckedIn);
            booking.CheckInTime = DateTime.UtcNow;  // record exact time guest arrived
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId;

            // ===== Update room status =====
            // room is now occupied - cant book it for someone else
            booking.Room.ChangeStatus(RoomStatus.Occupied);
            booking.Room.LastUpdatedAt = DateTime.UtcNow;
            booking.Room.LastUpdatedBy = command.User!.UserId;

            _context.Bookings.Update(booking);
            _context.Rooms.Update(booking.Room);

            await _context.SaveChangesAsync(cancellationToken);

            // ===== Issue digital key =====
            // guest can now unlock their room with phone!
            await _digitalKeyService.IssueKeyAsync(
                new IssueDigitalKeyCommand(BookingId: booking.Id) { User = command.User }, 
                cancellationToken);

            return true;
        }

        /// <summary>
        /// Check-out a guest - farewell time!
        /// This does A LOT of things:
        /// 1. Mark booking as checked out
        /// 2. Mark room as dirty (needs cleaning)
        /// 3. Create housekeeping task automatically
        /// 4. Revoke digital key (no more room access!)
        /// 5. Generate invoice PDF
        /// </summary>
        public async Task<bool> CheckOutBookingAsync(CheckOutBookingCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new InvalidOperationException("Booking not found.");

            // can only checkout if currently checked in
            if (booking.Status != BookingStatus.CheckedIn)
                throw new InvalidOperationException("Only checked-in bookings can be checked out.");

            // ===== Update booking status =====
            booking.ChangeStatus(BookingStatus.CheckedOut);
            booking.CheckOutTime = DateTime.UtcNow;  // record when they left
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId;

            // ===== Update room status → Dirty =====
            // housekeeping needs to clean before next guest
            booking.Room.ChangeStatus(RoomStatus.Dirty);
            booking.Room.LastUpdatedAt = DateTime.UtcNow;
            booking.Room.LastUpdatedBy = command.User!.UserId;

            _context.Bookings.Update(booking);
            _context.Rooms.Update(booking.Room);

            // ===== Auto-create housekeeping task =====
            // housekeeping staff sees this in their task list
            var cleaningTask = new HousekeepingTask
            {
                TaskName = $"Clean Room {booking.Room.RoomNumber}",
                Notes = $"Auto-generated after checkout of Booking #{booking.Id}",
                RoomId = booking.Room.Id,
                Status = TaskStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.User!.UserId
            };

            await _context.HousekeepingTasks.AddAsync(cleaningTask);

            // ===== Revoke digital key =====
            // guest cant open door anymore - security!
            await _digitalKeyService.RevokeKeyAsync(
                new RevokeDigitalKeyCommand(booking.Id) { User = command.User }, 
                cancellationToken);
            
            // ===== Generate invoice PDF =====
            // automatic! guest gets their bill ready to download
            await _invoiceServce.GenerateInvoiceAsync(
                new GenerateInvoiceCommand(command.BookingId) { User = command.User }, 
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        #endregion

        // ==================== GUEST SELF-SERVICE ====================
        // these are used by guests through mobile app / web portal
        
        #region Guest
        
        /// <summary>
        /// Get all bookings for a specific guest
        /// Guest app shows "My Reservations" screen with past and upcoming stays
        /// </summary>
        public async Task<List<BookingViewModel>> GetBookingsByGuestIdAsync(GetMyBookingsCommand command, CancellationToken cancellationToken)
        {
            // only get bookings that belong to THIS guest, not someone elses!
            return await _context.Bookings
                .Where(b => b.GuestProfileId == command.GuestId && !b.IsDeleted)
                .Include(b => b.Room)
                .Include(b => b.GuestProfile)
                .Select(b => new BookingViewModel(
                    b.Id,
                    b.ConfirmationCode,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.Status,
                    b.GuestProfile.FullName,
                    b.Room.RoomNumber,
                    b.TotalAmount,
                    null))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Quick status check - is my booking confirmed? checked in?
        /// Guest app polls this to update the UI
        /// </summary>
        public async Task<string> GetBookingStatusByIdAsync(GetBookingStatusByIdCommand command, CancellationToken cancellationToken)
        {
            // security check: make sure this booking belongs to the logged in user
            var status = await _context.Bookings
                .Where(b => b.Id == command.BookingId && b.GuestProfileId == command.User!.UserId && !b.IsDeleted)
                .Select(b => b.Status.ToString())
                .FirstOrDefaultAsync(cancellationToken);

            if (status == null)
                throw new KeyNotFoundException("Booking not found.");
            
            return status;
        }

        /// <summary>
        /// Guest self check-in - skip the front desk queue!
        /// Guest arrives, opens app, taps "Check In", and gets their digital key
        /// Very convenient for business travelers in a hurry
        /// </summary>
        public async Task<BookingViewModel> GuestCheckInAsync(GuestCheckInCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {command.BookingId} not found.");

            // SECURITY: verify this booking belongs to the person trying to check in
            // cant check into someone elses room!
            if (booking.GuestProfileId != command.GuestId)
                throw new UnauthorizedAccessException("You are not authorized to check in this booking.");

            // already checked in? no need to do it again
            if (booking.Status == BookingStatus.CheckedIn)
                throw new Exception("You have already checked in for this booking.");

            // can only check in a reserved booking
            if (booking.Status != BookingStatus.Reserved)
                throw new InvalidOperationException($"Cannot check in a booking with status '{booking.Status}'.");

            // ===== Update booking =====
            booking.ChangeStatus(BookingStatus.CheckedIn);
            booking.CheckInTime = DateTime.UtcNow;
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId;

            // ===== Update room =====
            booking.Room.ChangeStatus(RoomStatus.Occupied);
            booking.Room.LastUpdatedAt = DateTime.UtcNow;
            booking.Room.LastUpdatedBy = command.User!.UserId;

            _context.Bookings.Update(booking);
            _context.Rooms.Update(booking.Room);

            await _context.SaveChangesAsync(cancellationToken);

            // ===== Issue digital key =====
            // the magic moment - guest gets their room key on phone!
            var key = await _digitalKeyService.IssueKeyAsync(
                new IssueDigitalKeyCommand(BookingId: booking.Id) { User = command.User }, 
                cancellationToken);

            // return booking with digital key attached
            return new BookingViewModel(
                booking.Id,
                booking.ConfirmationCode,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.Status,
                booking.GuestProfile.FullName,
                booking.Room.RoomNumber,
                booking.TotalAmount,
                key  // here it is! guest shows this to open door
            );
        }

        /// <summary>
        /// Lookup booking by confirmation code + email
        /// Used at kiosks - guest doesnt need account, just their confirmation email
        /// Very useful for group bookings where one person booked for everyone
        /// </summary>
        public async Task<BookingViewModel> LookUpBookingAsync(LookupBookingCommand command, CancellationToken cancellationToken)
        {
            // both confirmation code AND email must match - security!
            var booking = await _context.Bookings
                .Include(b => b.GuestProfile)
                .Include(b => b.Room)
                .Where(b => b.ConfirmationCode == command.Code && b.GuestProfile.Email == command.Email && !b.IsDeleted)
                .Select(b => new BookingViewModel(
                    b.Id,
                    b.ConfirmationCode,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.Status,
                    b.GuestProfile.FullName,
                    b.Room.RoomNumber,
                    b.TotalAmount,
                    null
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (booking == null)
                throw new KeyNotFoundException("Booking not found with the provided confirmation code and email.");

            return booking;
        }
        #endregion
    }
}
