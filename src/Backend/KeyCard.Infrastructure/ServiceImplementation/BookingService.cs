using KeyCard.BusinessLogic.Commands.Admin.Bookings;
using KeyCard.BusinessLogic.Commands.Admin.DigitalKey;
using KeyCard.BusinessLogic.Commands.Guest.Bookings;
using KeyCard.BusinessLogic.Commands.Guest.Invoice;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models;
using KeyCard.Infrastructure.Models.AppDbContext;
using KeyCard.Infrastructure.Models.Bookings;
using KeyCard.Infrastructure.Models.HouseKeeping;
using KeyCard.Infrastructure.Models.User;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    public class BookingService : IBookingService
    {

        private readonly ApplicationDBContext _context;
        private readonly IDigitalKeyService _digitalKeyService;
        private readonly IInvoiceService _invoiceServce;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationUserRole> _roleManager;

        public BookingService(
            ApplicationDBContext context, 
            IDigitalKeyService digitalKeyService, 
            IInvoiceService invoiceServce,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationUserRole> roleManager)
        {
            _context = context;
            _digitalKeyService = digitalKeyService;
            _invoiceServce = invoiceServce;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        #region Admin
        public async Task<BookingViewModel> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken)
        {
            // Validate Room availability
            bool isAvailable = !await _context.Bookings.AnyAsync(
                b => b.RoomId == command.RoomId &&
                     b.Status != BookingStatus.Cancelled &&
                     b.CheckInDate < command.CheckOutDate &&
                     b.CheckOutDate > command.CheckInDate &&
                     !b.IsDeleted,
                cancellationToken);

            if (!isAvailable)
                throw new InvalidOperationException("Room is not available for the selected dates.");

            var nights = Math.Max(1, (command.CheckOutDate.Date - command.CheckInDate.Date).Days);

            var baseRate = await _context.Rooms
                .Where(r => r.Id == command.RoomId)
                .Select(r => r.RoomType.BaseRate)
                .FirstAsync(cancellationToken);
            var totalAmount = baseRate * nights;

            var booking = new Booking
            {
                ConfirmationCode = $"KCN-{Random.Shared.Next(100000, 999999)}",
                GuestProfileId = command.GuestProfileId,
                RoomId = command.RoomId,
                CheckInDate = command.CheckInDate,
                CheckOutDate = command.CheckOutDate,
                Adults = command.Adults,
                Children = command.Children,
                IsPrepaid = command.IsPrepaid,
                Status = BookingStatus.Reserved,
                TotalAmount = totalAmount,
                CreatedBy = command.User!.UserId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedBy = command.User!.UserId
            };

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync(cancellationToken);

            // If prepaid, create payment entry
            if (command.IsPrepaid)
            {
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Amount = totalAmount,
                    PaidAt = DateTime.UtcNow,
                    Method = "Online",
                    TransactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = command.User!.UserId
                };

                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync(cancellationToken);
            }

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
                null
            );
        }

        public async Task<BookingViewModel> GetBookingByIdAsync(GetBookingByIdCommand command, CancellationToken cancellationToken)
        {
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

            if(booking == null)
                throw new KeyNotFoundException("Booking not found.");

            return booking;
        }

        public async Task<List<BookingViewModel>> GetAllBookingsAsync(GetAllBookingsCommand command, CancellationToken cancellationToken)
        {
            var bookingsQuery = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.GuestProfile)
                .Where(b => !b.IsDeleted)
                .AsQueryable();

            if (command.FromDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.CheckInDate >= command.FromDate.Value);

            if (command.ToDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.CheckInDate <= command.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(command.Status) &&
                Enum.TryParse<BookingStatus>(command.Status, true, out var status))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(command.GuestName))
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.GuestProfile.FullName.ToLower().Contains(command.GuestName.ToLower()));
            }

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

        public async Task<bool> CancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                return false;

            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut)
                throw new InvalidOperationException("Cannot cancel a booking that has already been checked in or checked out.");

            // Cancel booking
            booking.ChangeStatus(BookingStatus.Cancelled);
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId;
            _context.Bookings.Update(booking);

            // Free the room if no other active booking overlaps
            bool hasOtherActive = await _context.Bookings.AnyAsync(b =>
                b.RoomId == booking.RoomId &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.CheckedOut &&
                b.Id != booking.Id, cancellationToken);

            if (!hasOtherActive)
            {
                booking.Room.ChangeStatus(RoomStatus.Vacant);
                _context.Rooms.Update(booking.Room);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> CheckInBookingAsync(CheckInBookingCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new InvalidOperationException("Booking not found.");

            if (booking.Status != BookingStatus.Reserved)
                throw new InvalidOperationException("Only reserved bookings can be checked in.");

            // Update booking status
            booking.ChangeStatus(BookingStatus.CheckedIn);
            booking.CheckInTime = DateTime.UtcNow;
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId; // later replace with current user via IUserContext

            // Update room status
            booking.Room.ChangeStatus(RoomStatus.Occupied);
            booking.Room.LastUpdatedAt = DateTime.UtcNow;
            booking.Room.LastUpdatedBy = command.User!.UserId;

            _context.Bookings.Update(booking);
            _context.Rooms.Update(booking.Room);

            await _context.SaveChangesAsync(cancellationToken);

            await _digitalKeyService.IssueKeyAsync(new IssueDigitalKeyCommand(BookingId: booking.Id) { User = command.User}, cancellationToken);

            return true;
        }

        public async Task<bool> CheckOutBookingAsync(CheckOutBookingCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new InvalidOperationException("Booking not found.");

            if (booking.Status != BookingStatus.CheckedIn)
                throw new InvalidOperationException("Only checked-in bookings can be checked out.");

            // Update booking status
            booking.ChangeStatus(BookingStatus.CheckedOut);
            booking.CheckOutTime = DateTime.UtcNow;
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId; // later: replace with IUserContext

            // Update room status → Dirty
            booking.Room.ChangeStatus(RoomStatus.Dirty);
            booking.Room.LastUpdatedAt = DateTime.UtcNow;
            booking.Room.LastUpdatedBy = command.User!.UserId;

            _context.Bookings.Update(booking);
            _context.Rooms.Update(booking.Room);

            //create housekeeping task for cleaning the room
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

            // (Future: Generate invoice or revoke digital key here)
            await _digitalKeyService.RevokeKeyAsync(new RevokeDigitalKeyCommand(booking.Id) { User = command.User }, cancellationToken);
            await _invoiceServce.GenerateInvoiceAsync(new GenerateInvoiceCommand (command.BookingId)
            {
                User = command.User
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        #endregion

        #region Guest
        
        public async Task<BookingViewModel> CreateGuestBookingAsync(CreateGuestBookingCommand command, CancellationToken cancellationToken)
        {
            // Find all vacant rooms of the given room type
            var vacantRooms = await _context.Rooms
                .Where(r => r.RoomTypeId == command.RoomTypeId &&
                           !r.IsDeleted &&
                           r.Status == RoomStatus.Vacant)
                .Include(r => r.RoomType)
                .ToListAsync(cancellationToken);

            if (vacantRooms.Count == 0)
                throw new InvalidOperationException("No available rooms for the selected room type.");

            // Get all room IDs that have conflicting bookings for the requested dates
            var bookedRoomIds = await _context.Bookings
                .Where(b => vacantRooms.Select(r => r.Id).Contains(b.RoomId) &&
                           b.Status != BookingStatus.Cancelled &&
                           b.CheckInDate < command.CheckOutDate &&
                           b.CheckOutDate > command.CheckInDate &&
                           !b.IsDeleted)
                .Select(b => b.RoomId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Find the first room that is NOT booked for these dates
            var availableRoom = vacantRooms.FirstOrDefault(r => !bookedRoomIds.Contains(r.Id));

            if (availableRoom == null)
                throw new InvalidOperationException("All rooms of this type are booked for the selected dates.");

            // Determine the guest ID - either from logged in user or from email
            int guestId;
            string guestName;

            if (command.User?.UserId > 0)
            {
                // User is logged in
                guestId = command.User.UserId;
                var existingGuest = await _context.Users.FirstAsync(g => g.Id == guestId, cancellationToken);
                guestName = existingGuest.FullName;
            }
            else if (!string.IsNullOrEmpty(command.GuestEmail))
            {
                // Find or create guest by email
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == command.GuestEmail, cancellationToken);
                
                if (existingUser != null)
                {
                    guestId = existingUser.Id;
                    guestName = existingUser.FullName;
                }
                else
                {
                    // Create a new guest user using Identity
                    var newGuest = new ApplicationUser
                    {
                        UserName = command.GuestEmail,
                        Email = command.GuestEmail,
                        FirstName = command.GuestFirstName ?? "Guest",
                        LastName = command.GuestLastName ?? "",
                        FullName = $"{command.GuestFirstName ?? "Guest"} {command.GuestLastName ?? ""}".Trim(),
                        EmailConfirmed = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(newGuest, "Guest@123");
                    if (!result.Succeeded)
                        throw new InvalidOperationException($"Failed to create guest account: {string.Join("; ", result.Errors.Select(e => e.Description))}");

                    // Ensure Guest role exists and assign it
                    if (!await _roleManager.RoleExistsAsync("Guest"))
                        await _roleManager.CreateAsync(new ApplicationUserRole("Guest"));
                    
                    await _userManager.AddToRoleAsync(newGuest, "Guest");
                    
                    guestId = newGuest.Id;
                    guestName = newGuest.FullName;
                }
            }
            else
            {
                throw new InvalidOperationException("Guest email is required for booking.");
            }

            var nights = Math.Max(1, (command.CheckOutDate.Date - command.CheckInDate.Date).Days);

            var totalAmount = availableRoom.RoomType.BaseRate * nights;

            var booking = new Booking
            {
                ConfirmationCode = $"KCN-{Random.Shared.Next(100000, 999999)}",
                GuestProfileId = guestId,
                RoomId = availableRoom.Id,
                CheckInDate = command.CheckInDate,
                CheckOutDate = command.CheckOutDate,
                Adults = command.Adults,
                Children = command.Children,
                IsPrepaid = command.IsPrepaid,
                Status = BookingStatus.Reserved,
                TotalAmount = totalAmount,
                CreatedBy = guestId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedBy = guestId
            };

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync(cancellationToken);

            // If prepaid, create payment entry
            if (command.IsPrepaid)
            {
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Amount = totalAmount,
                    PaidAt = DateTime.UtcNow,
                    Method = "Online",
                    TransactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = guestId
                };

                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new BookingViewModel(
                booking.Id,
                booking.ConfirmationCode,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.Status,
                guestName,
                availableRoom.RoomNumber,
                booking.TotalAmount,
                null
            );
        }

        public async Task<List<BookingViewModel>> GetBookingsByGuestIdAsync(GetMyBookingsCommand command, CancellationToken cancellationToken)
        {
            var bookings = await _context.Bookings
                .Where(b => b.GuestProfileId == command.GuestId && !b.IsDeleted)
                .Include(b => b.Room)
                .Include(b => b.GuestProfile)
                .Include(b => b.DigitalKey)
                .ToListAsync(cancellationToken);

            return bookings.Select(b => new BookingViewModel(
                b.Id,
                b.ConfirmationCode,
                b.CheckInDate,
                b.CheckOutDate,
                b.Status,
                b.GuestProfile.FullName,
                b.Room.RoomNumber,
                b.TotalAmount,
                b.DigitalKey != null && !b.DigitalKey.IsRevoked
                    ? new DigitalKeyViewModel
                    {
                        Id = b.DigitalKey.Id,
                        Token = b.DigitalKey.Token,
                        IssuedAt = b.DigitalKey.IssuedAt,
                        ExpiresAt = b.DigitalKey.ExpiresAt,
                        IsRevoked = b.DigitalKey.IsRevoked,
                        BookingId = b.DigitalKey.BookingId
                    }
                    : null
            )).ToList();
        }

        public async Task<string> GetBookingStatusByIdAsync(GetBookingStatusByIdCommand command, CancellationToken cancellationToken)
        {
            var status = await _context.Bookings
                .Where(b => b.Id == command.BookingId && b.GuestProfileId == command.User!.UserId && !b.IsDeleted)
                .Select(b => b.Status.ToString())
                .FirstOrDefaultAsync(cancellationToken);

            if (status == null)
                throw new KeyNotFoundException("Booking not found.");
            
            return status;
        }

        public async Task<BookingViewModel> GuestCheckInAsync(GuestCheckInCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.GuestProfile)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {command.BookingId} not found.");

            if (booking.GuestProfileId != command.GuestId)
                throw new UnauthorizedAccessException("You are not authorized to check in this booking.");

            if (booking.Status == BookingStatus.CheckedIn)
                throw new Exception("You have already checked in for this booking.");

            if (booking.Status != BookingStatus.Reserved)
                throw new InvalidOperationException($"Cannot check in a booking with status '{booking.Status}'.");

            // Update booking status
            booking.ChangeStatus(BookingStatus.CheckedIn);
            booking.CheckInTime = DateTime.UtcNow;
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId;

            // Update room status
            booking.Room.ChangeStatus(RoomStatus.Occupied);
            booking.Room.LastUpdatedAt = DateTime.UtcNow;
            booking.Room.LastUpdatedBy = command.User!.UserId;

            _context.Bookings.Update(booking);
            _context.Rooms.Update(booking.Room);

            await _context.SaveChangesAsync(cancellationToken);

            var key = await _digitalKeyService.IssueKeyAsync(new IssueDigitalKeyCommand(BookingId: booking.Id) { User = command.User }, cancellationToken);

            return new BookingViewModel(
                    booking.Id,
                    booking.ConfirmationCode,
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    booking.Status,
                    booking.GuestProfile.FullName,
                    booking.Room.RoomNumber,
                    booking.TotalAmount,
                    key);
           
        }

        public async Task<BookingViewModel> GuestCheckOutAsync(GuestCheckOutCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.GuestProfile)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {command.BookingId} not found.");

            if (booking.GuestProfileId != command.GuestId)
                throw new UnauthorizedAccessException("You are not authorized to check out this booking.");

            if (booking.Status != BookingStatus.CheckedIn)
                throw new InvalidOperationException($"Cannot check out a booking with status '{booking.Status}'. Only checked-in bookings can be checked out.");

            // Update booking status
            booking.ChangeStatus(BookingStatus.CheckedOut);
            booking.CheckOutTime = DateTime.UtcNow;
            booking.LastUpdatedAt = DateTime.UtcNow;
            booking.LastUpdatedBy = command.User!.UserId;

            // Update room status → Dirty
            booking.Room.ChangeStatus(RoomStatus.Dirty);
            booking.Room.LastUpdatedAt = DateTime.UtcNow;
            booking.Room.LastUpdatedBy = command.User!.UserId;

            _context.Bookings.Update(booking);
            _context.Rooms.Update(booking.Room);

            // Create housekeeping task for cleaning the room
            var cleaningTask = new HousekeepingTask
            {
                TaskName = $"Clean Room {booking.Room.RoomNumber}",
                Notes = $"Auto-generated after guest checkout of Booking #{booking.Id}",
                RoomId = booking.Room.Id,
                Status = TaskStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.User!.UserId
            };

            await _context.HousekeepingTasks.AddAsync(cleaningTask);

            // Revoke digital key
            await _digitalKeyService.RevokeKeyAsync(new RevokeDigitalKeyCommand(booking.Id) { User = command.User }, cancellationToken);
            
            // Generate invoice
            await _invoiceServce.GenerateInvoiceAsync(new GenerateInvoiceCommand(command.BookingId) { User = command.User }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return new BookingViewModel(
                booking.Id,
                booking.ConfirmationCode,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.Status,
                booking.GuestProfile.FullName,
                booking.Room.RoomNumber,
                booking.TotalAmount,
                null // Digital key revoked
            );
        }

        public async Task<BookingViewModel> LookUpBookingAsync(LookupBookingCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.GuestProfile)
                .Include(b => b.Room)
                .Include(b => b.DigitalKey)
                .Where(b => b.ConfirmationCode == command.Code && b.GuestProfile.Email == command.Email && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (booking == null)
                throw new KeyNotFoundException("Booking not found with the provided confirmation code and email.");

            // Map digital key if exists and not revoked
            DigitalKeyViewModel? digitalKeyVm = null;
            if (booking.DigitalKey != null && !booking.DigitalKey.IsRevoked)
            {
                digitalKeyVm = new DigitalKeyViewModel
                {
                    Id = booking.DigitalKey.Id,
                    Token = booking.DigitalKey.Token,
                    IssuedAt = booking.DigitalKey.IssuedAt,
                    ExpiresAt = booking.DigitalKey.ExpiresAt,
                    IsRevoked = booking.DigitalKey.IsRevoked,
                    BookingId = booking.DigitalKey.BookingId
                };
            }

            return new BookingViewModel(
                booking.Id,
                booking.ConfirmationCode,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.Status,
                booking.GuestProfile.FullName,
                booking.Room.RoomNumber,
                booking.TotalAmount,
                digitalKeyVm
            );
        }
        #endregion
    }
}
