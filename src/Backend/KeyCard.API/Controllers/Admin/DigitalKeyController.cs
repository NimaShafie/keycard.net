using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.Commands.Admin.DigitalKey;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.UserClaims;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Admin
{
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

        [HttpPost("{id:int}/IssueKey")]
        [Authorize(Roles = "FrontDesk,Admin")]
        public async Task<ActionResult<DigitalKeyViewModel>> IssueKey(int id)
        {
            var command = new IssueDigitalKeyCommand(id);
            command.User = _contextAccessor.HttpContext?.User.GetUser();

            var dto = await _mediator.Send(command);
            return Ok(dto);
        }

        [HttpPost("{id:int}/RevokeKey")]
        [Authorize(Roles = "FrontDesk,Admin")]
        public async Task<ActionResult> RevokeKey(int id)
        {
            var command = new RevokeDigitalKeyCommand(id);
            command.User = _contextAccessor.HttpContext?.User.GetUser();

            var success = await _mediator.Send(command);
            return success ? Ok(new { message = "Key revoked." }) : NotFound(new { message = "Active key not found." });
        }

        [HttpGet("{id:int}/GetDigitalKey")]
        //[Authorize(Roles = "Guest")]
        public async Task<ActionResult> GetDigitalKey(int id)
        {
            var dto = await _mediator.Send(new GetDigitalKeyByBookingIdCommand(id));
            return Ok(dto);
        }
    }
}
