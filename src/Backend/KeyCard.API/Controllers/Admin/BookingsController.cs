using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.Commands.Admin.Bookings;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.UserClaims;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "FrontDesk,Admin")]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly UserClaimsViewModel? _user;
    public BookingsController(IMediator mediator, IHttpContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
        _user = _contextAccessor.HttpContext!.User.GetUser();
    }

    [HttpPost]
    public async Task<ActionResult<BookingViewModel>> CreateBooking([FromBody] CreateBookingCommand request)
    {
        request.User = _user;
        var result = await _mediator.Send(request);
        return Ok(result);
    }

    // GET /api/v1/bookings/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingViewModel>> GetBookingById(int id)
    {
        var command = new GetBookingByIdCommand(id);
        command.User = _user;

        var result = await _mediator.Send(command);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("GetAllBookings")]
    public async Task<ActionResult<IEnumerable<BookingViewModel>>> GetAllBookings(
    [FromQuery] DateTime? fromDate,
    [FromQuery] DateTime? toDate,
    [FromQuery] string? status,
    [FromQuery] string? guestName)
    {
        var command = new GetAllBookingsCommand(fromDate, toDate, status, guestName);
        command.User = _user;

        var result = await _mediator.Send(command);
        return Ok(result);
    }

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

    [HttpPost("{id:int}/checkin")]
    public async Task<ActionResult> CheckIn(int id)
    {
        var success = await _mediator.Send(new CheckInBookingCommand(id) { User = _user });

        if (!success)
            return BadRequest(new { message = "Check-in failed or booking not found." });

        return Ok(new { message = "Guest checked in successfully." });
    }

    [HttpPost("{id:int}/checkout")]
    public async Task<ActionResult> CheckOut(int id)
    {
        var success = await _mediator.Send(new CheckOutBookingCommand(id) { User = _user });

        if (!success)
            return BadRequest(new { message = "Check-out failed or booking not found." });

        return Ok(new { message = "Guest checked out successfully. Room marked as Dirty." });
    }


}
