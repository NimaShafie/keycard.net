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
        private readonly UserClaimsViewModel? _user;
        public DigitalKeyController(IMediator mediator, IHttpContextAccessor contextAccessor)
        {
            _mediator = mediator;
            _contextAccessor = contextAccessor;
            _user = _contextAccessor.HttpContext!.User.GetUser();
        }

        [HttpPost("{id:int}/key")]
        [Authorize(Roles = "FrontDesk,Admin")]
        public async Task<ActionResult<DigitalKeyViewModel>> IssueKey(int id)
        {
            var dto = await _mediator.Send(new IssueDigitalKeyCommand(id));
            return Ok(dto);
        }

        [HttpPost("{id:int}/key/revoke")]
        [Authorize(Roles = "FrontDesk,Admin")]
        public async Task<ActionResult> RevokeKey(int id)
        {
            var success = await _mediator.Send(new RevokeDigitalKeyCommand(id));
            return success ? Ok(new { message = "Key revoked." }) : NotFound(new { message = "Active key not found." });
        }
    }
}
