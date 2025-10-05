using KeyCard.BusinessLogic.Commands.Bookings;
using KeyCard.BusinessLogic.ViewModels;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    //[Authorize(Roles = "FrontDesk,Admin,Guest")]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingCommand request)
    {
        var result = await _mediator.Send(request);
        return Ok(result);
    }

    // GET /api/v1/bookings/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDto>> GetBookingById(int id)
    {
        var result = await _mediator.Send(new GetBookingByIdCommand(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("GetAllBookings")]
    //[Authorize(Roles = "FrontDesk,Admin")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllBookings(
    [FromQuery] DateTime? fromDate,
    [FromQuery] DateTime? toDate,
    [FromQuery] string? status,
    [FromQuery] string? guestName)
    {
        var result = await _mediator.Send(new GetAllBookingsCommand(fromDate, toDate, status, guestName));
        return Ok(result);
    }

    [HttpPost("{id:int}/CancelBooking")]
    [Authorize(Roles = "FrontDesk,Guest,Admin")]
    public async Task<ActionResult> CancelBooking(int id)
    {
        var success = await _mediator.Send(new CancelBookingCommand(id));

        if (!success)
            return NotFound(new { message = "Booking not found or already processed." });

        return Ok(new { message = "Booking cancelled successfully." });
    }


}
