using KeyCard.BusinessLogic.Commands.Bookings;
using KeyCard.BusinessLogic.Commands.DigitalKey;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.AppDbContext;
using KeyCard.Infrastructure.Models.Bookings;
using KeyCard.Infrastructure.Models.HouseKeeping;

using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    public class BookingService : IBookingService
    {

        private readonly ApplicationDBContext _context;
        private readonly IDigitalKeyService _digitalKeyService;

        public BookingService(ApplicationDBContext context, IDigitalKeyService digitalKeyService)
        {
            _context = context;
            _digitalKeyService = digitalKeyService;
        }

        public async Task<BookingViewModel> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken)
        {
            // Validate Room availability
            bool isAvailable = !await _context.Bookings.AnyAsync(
                b => b.RoomId == command.RoomId &&
                     b.Status != BookingStatus.Cancelled &&
                     b.CheckInDate < command.CheckOutDate &&
                     b.CheckOutDate > command.CheckInDate,
                cancellationToken);

            if (!isAvailable)
                throw new InvalidOperationException("Room is not available for the selected dates.");

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
                TotalAmount = await _context.Rooms
                    .Where(r => r.Id == command.RoomId)
                    .Select(r => r.RoomType.BaseRate)
                    .FirstAsync(cancellationToken),
                CreatedBy = command.User!.UserId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedBy = command.User!.UserId
            };

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync(cancellationToken);

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
                booking.TotalAmount
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
                    b.TotalAmount
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
                    b.TotalAmount
                ))
                .ToListAsync(cancellationToken);

            return list;
        }

        public async Task<bool> CancelBookingAsync(CancelBookingCommand command, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId, cancellationToken);

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
                
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }



    }
}
