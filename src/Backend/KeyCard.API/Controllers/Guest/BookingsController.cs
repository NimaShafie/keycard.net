// ============================================================================
// GUEST BOOKINGS CONTROLLER - SELF-SERVICE FOR HOTEL GUESTS
// guests can view their bookings, check themselves in, download invoices
// this powers the guest mobile app and web portal
// ============================================================================

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
    /// <summary>
    /// Guest self-service endpoints
    /// Guests can manage their own bookings without calling front desk
    /// Very convenient - check in from phone while still in taxi!
    /// </summary>
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
        /// Get all my bookings - past, current, and upcoming
        /// Guest can see their booking history in the app
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Guest")]  // only logged in guests
        public async Task<IActionResult> GetMyBookings(CancellationToken cancellationToken)
        {
            // GetUserId() extracts user ID from JWT token claims
            var result = await _mediator.Send(new GetMyBookingsCommand(User.GetUserId()) { User = User.GetUser() });
            return Ok(result);
        }

        /// <summary>
        /// Lookup booking without logging in - using confirmation code + email
        /// Perfect for hotel lobby kiosks! Guest types code, sees their booking
        /// No account needed, just the confirmation email they received
        /// </summary>
        [AllowAnonymous]  // anyone can try to lookup (with correct code+email)
        [HttpGet("lookup")]
        public async Task<IActionResult> LookupBooking([FromQuery] string code, [FromQuery] string email, CancellationToken cancellationToken)
        {
            var command = new LookupBookingCommand(code, email);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Self check-in - guest checks themselves in without waiting at front desk
        /// They get digital room key immediately after!
        /// Much faster than traditional check-in queue
        /// </summary>
        [Authorize(Roles = "Guest")]
        [HttpPost("{bookingId:int}/checkin")]
        public async Task<IActionResult> GuestCheckIn(int bookingId, CancellationToken cancellationToken)
        {
            // we verify that this booking belongs to the logged-in guest
            var command = new GuestCheckInCommand(bookingId, User.GetUserId()) { User = User.GetUser() };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Check booking status - is it reserved? checked in? 
        /// App shows different screens based on status
        /// </summary>
        [Authorize(Roles = "Guest")]
        [HttpGet("{bookingId:int}/status")]
        public async Task<IActionResult> GetBookingStatusById(int bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBookingStatusByIdCommand(bookingId) { User = User.GetUser() }, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Download invoice PDF - guests and staff can get the invoice
        /// Generated automatically at checkout
        /// File is streamed directly, ready to print or email
        /// </summary>
        [HttpGet("{bookingId:int}/invoice")]
        [Authorize(Roles = "Guest,FrontDesk,Admin")]  // guest sees their own, staff sees all
        public async Task<IActionResult> GetInvoicePdf(int bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetInvoicePdfCommand(bookingId), cancellationToken);

            // build full path to the PDF file
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", result.PdfPath.TrimStart('/'));

            // make sure file actually exists on disk
            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found.");

            // stream the PDF - browser will download or display it
            var stream = System.IO.File.OpenRead(fullPath);
            return File(stream, "application/pdf", Path.GetFileName(fullPath));
        }
    }
}
