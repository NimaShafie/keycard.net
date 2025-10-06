using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.Commands.Bookings;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.UserClaims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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
    [Authorize(Roles = "FrontDesk,Admin,Guest")]
    public async Task<ActionResult<BookingViewModel>> CreateBooking([FromBody] CreateBookingCommand request)
    {
        request.User = this._user;
        var result = await _mediator.Send(request);
        return Ok(result);
    }

    // GET /api/v1/bookings/{id}
    [HttpGet("{id:int}")]
    [Authorize(Roles = "FrontDesk,Admin,Guest")]
    public async Task<ActionResult<BookingViewModel>> GetBookingById(int id)
    {
        GetBookingByIdCommand command = new GetBookingByIdCommand(id);
        command.User = this._user;

        var result = await _mediator.Send(command);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("GetAllBookings")]
    [Authorize(Roles = "FrontDesk,Admin")]
    public async Task<ActionResult<IEnumerable<BookingViewModel>>> GetAllBookings(
    [FromQuery] DateTime? fromDate,
    [FromQuery] DateTime? toDate,
    [FromQuery] string? status,
    [FromQuery] string? guestName)
    {
        GetAllBookingsCommand command = new GetAllBookingsCommand(fromDate, toDate, status, guestName);
        command.User = this._user;

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{id:int}/CancelBooking")]
    [Authorize(Roles = "FrontDesk,Guest,Admin")]
    public async Task<ActionResult> CancelBooking(int id)
    {
        var command = new CancelBookingCommand(id);
        command.User = this._user;

        var success = await _mediator.Send(command);

        if (!success)
            return NotFound(new { message = "Booking not found or already processed." });

        return Ok(new { message = "Booking cancelled successfully." });
    }


}
