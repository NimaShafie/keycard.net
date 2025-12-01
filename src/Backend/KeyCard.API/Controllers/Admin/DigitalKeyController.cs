// ============================================================================
// DIGITAL KEY CONTROLLER - MOBILE ROOM ACCESS
// this is the cool modern feature! guests use phone to unlock room door
// no more plastic key cards that get demagnetized or lost
// ============================================================================

using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.Commands.Admin.DigitalKey;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.UserClaims;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Admin
{
    /// <summary>
    /// Digital key management - issue and revoke mobile room keys
    /// Guest shows QR code or uses NFC to unlock door
    /// Very secure - key has unique token and expiration date
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    public class DigitalKeyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _contextAccessor;
        
        public DigitalKeyController(IMediator mediator, IHttpContextAccessor contextAccessor)
        {
            _mediator = mediator;
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Issue a digital key for a booking
        /// Usually happens automatically at check-in, but staff can manually issue too
        /// Key expires at checkout date
        /// </summary>
        [HttpPost("{id:int}/IssueKey")]
        [Authorize(Roles = "FrontDesk,Admin")]
        public async Task<ActionResult<DigitalKeyViewModel>> IssueKey(int id)
        {
            var command = new IssueDigitalKeyCommand(id);
            command.User = _contextAccessor.HttpContext?.User.GetUser();

            var dto = await _mediator.Send(command);
            return Ok(dto);
        }

        /// <summary>
        /// Revoke a digital key - key becomes invalid immediately
        /// Used when guest checks out or if key is compromised
        /// Security first! Once revoked, door wont open anymore
        /// </summary>
        [HttpPost("{id:int}/RevokeKey")]
        [Authorize(Roles = "FrontDesk,Admin")]
        public async Task<ActionResult> RevokeKey(int id)
        {
            var command = new RevokeDigitalKeyCommand(id);
            command.User = _contextAccessor.HttpContext?.User.GetUser();

            var success = await _mediator.Send(command);
            return success ? Ok(new { message = "Key revoked." }) : NotFound(new { message = "Active key not found." });
        }

        /// <summary>
        /// Get digital key details for a booking
        /// Returns token, issue date, expiry date
        /// Guest app calls this to show the key on screen
        /// </summary>
        [HttpGet("{id:int}/GetDigitalKey")]
        // TODO: uncomment this when guest app integration is ready
        //[Authorize(Roles = "Guest")]
        public async Task<ActionResult> GetDigitalKey(int id)
        {
            var dto = await _mediator.Send(new GetDigitalKeyByBookingIdCommand(id));
            return Ok(dto);
        }
    }
}
