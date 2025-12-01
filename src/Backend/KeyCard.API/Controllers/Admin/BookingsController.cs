// ============================================================================
// ADMIN BOOKINGS CONTROLLER - FRONT DESK OPERATIONS
// this is where the hotel staff does all the booking magic
// create reservations, check guests in/out, cancel bookings, etc.
// ============================================================================

using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.Commands.Admin.Bookings;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.UserClaims;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Admin;

/// <summary>
/// Bookings management for hotel staff
/// Only FrontDesk and Admin roles can access these endpoints
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "FrontDesk,Admin")]  // regular guests cant touch this!
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly UserClaimsViewModel? _user;  // current logged in user info
    
    public BookingsController(IMediator mediator, IHttpContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
        // extract user info from JWT claims - we need to know WHO is doing what
        _user = _contextAccessor.HttpContext!.User.GetUser();
    }

    /// <summary>
    /// Create a new booking - front desk staff making reservation for walk-in guest
    /// or booking over phone
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingViewModel>> CreateBooking([FromBody] CreateBookingCommand request)
    {
        // attach user info so we know who created this booking
        request.User = _user;
        var result = await _mediator.Send(request);
        return Ok(result);
    }

    /// <summary>
    /// Get single booking by ID
    /// Staff needs to look up booking details when guest arrives
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingViewModel>> GetBookingById(int id)
    {
        var command = new GetBookingByIdCommand(id);
        command.User = _user;

        var result = await _mediator.Send(command);
        if (result == null) return NotFound();  // booking not exist or deleted
        return Ok(result);
    }

    /// <summary>
    /// Get all bookings with optional filters
    /// Used in dashboard to show todays arrivals, departures, etc.
    /// </summary>
    [HttpGet("GetAllBookings")]
    public async Task<ActionResult<IEnumerable<BookingViewModel>>> GetAllBookings(
        [FromQuery] DateTime? fromDate,   // filter by check-in date range
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,       // Reserved, CheckedIn, CheckedOut, Cancelled
        [FromQuery] string? guestName)    // search by guest name
    {
        var command = new GetAllBookingsCommand(fromDate, toDate, status, guestName);
        command.User = _user;

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a booking - guest changed their mind or double booking
    /// Cannot cancel if already checked in!
    /// </summary>
    [HttpPost("{id:int}/CancelBooking")]
    public async Task<ActionResult> CancelBooking(int id)
    {
        var command = new CancelBookingCommand(id);
        command.User = _user;

        var success = await _mediator.Send(command);

        if (!success)
            return NotFound(new { message = "Booking not found or already processed." });

        return Ok(new { message = "Booking cancelled successfully." });
    }

    /// <summary>
    /// Check-in a guest - the exciting moment when guest gets their room!
    /// This also issues digital key automatically
    /// </summary>
    [HttpPost("{id:int}/checkin")]
    public async Task<ActionResult> CheckIn(int id)
    {
        var success = await _mediator.Send(new CheckInBookingCommand(id) { User = _user });

        if (!success)
            return BadRequest(new { message = "Check-in failed or booking not found." });

        return Ok(new { message = "Guest checked in successfully." });
    }

    /// <summary>
    /// Check-out a guest - farewell time!
    /// This revokes digital key, marks room as dirty, creates housekeeping task
    /// and generates invoice automatically
    /// </summary>
    [HttpPost("{id:int}/checkout")]
    public async Task<ActionResult> CheckOut(int id)
    {
        var success = await _mediator.Send(new CheckOutBookingCommand(id) { User = _user });

        if (!success)
            return BadRequest(new { message = "Check-out failed or booking not found." });

        // room gets marked as Dirty - housekeeping will clean it
        return Ok(new { message = "Guest checked out successfully. Room marked as Dirty." });
    }
}
