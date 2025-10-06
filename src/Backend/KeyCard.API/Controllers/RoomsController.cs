using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.ViewModels.UserClaims;

using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers
{
    public class RoomsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserClaimsViewModel _user;

        public RoomsController(IMediator mediator, IHttpContextAccessor contextAccessor)
        {
            _mediator = mediator;
            _contextAccessor = contextAccessor;
            //_user = _contextAccessor.HttpContext!.User.GetUser();
        }
        public IActionResult Index()
        {
            return Ok();
        }
    }
}
