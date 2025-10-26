using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.Commands.Guest.Bookings;
using KeyCard.BusinessLogic.Commands.Guest.Invoice;
using KeyCard.Infrastructure.Models;
using KeyCard.Infrastructure.Models.AppDbContext;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Guest
{
    [ApiController]
    [Route("api/guest/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BookingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gets all bookings for the currently logged-in guest.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetMyBookings(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send( new GetMyBookingsCommand(User.GetUserId()) { User = User.GetUser() });
            return Ok(result);
        }

        /// <summary>
        /// Lookup booking by confirmation code or email (pre-login, kiosk use).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("lookup")]
        public async Task<IActionResult> LookupBooking([FromQuery] string code, [FromQuery] string email, CancellationToken cancellationToken)
        {
            var command = new LookupBookingCommand(code, email);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Check in the currently authenticated guest’s booking.
        /// </summary>
        [Authorize(Roles = "Guest")]
        [HttpPost("{bookingId:int}/checkin")]
        public async Task<IActionResult> GuestCheckIn(int bookingId, CancellationToken cancellationToken)
        {
            var command = new GuestCheckInCommand(bookingId, User.GetUserId()) { User = User.GetUser()};
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get booking status.
        /// </summary>
        [Authorize(Roles = "Guest")]
        [HttpGet("{bookingId:int}/status")]
        public async Task<IActionResult> GetBookingStatusById(int bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBookingStatusByIdCommand(bookingId) { User = User.GetUser() }, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{bookingId:int}/invoice")]
        [Authorize(Roles = "Guest,FrontDesk,Admin")]

        public async Task<IActionResult> GetInvoicePdf(int bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetInvoicePdfCommand(bookingId), cancellationToken);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", result.PdfPath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found.");

            var stream = System.IO.File.OpenRead(fullPath);
            return File(stream, "application/pdf", Path.GetFileName(fullPath));
        }

    }
}
