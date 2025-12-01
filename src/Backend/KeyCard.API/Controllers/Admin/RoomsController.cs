// ============================================================================
// ADMIN ROOMS CONTROLLER - ROOM MANAGEMENT
// for staff to manage rooms, check availability, update status
// currently a bit empty but ready for more features!
// ============================================================================

using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.ViewModels.UserClaims;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Admin
{
    /// <summary>
    /// Room management endpoints for hotel staff
    /// Admin, HouseKeeping, and Employee roles can access
    /// TODO: add more endpoints like UpdateRoomStatus, GetRoomsByFloor, etc.
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin,HouseKeeping,Employee")]
    public class RoomsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserClaimsViewModel _user;

        public RoomsController(IMediator mediator, IHttpContextAccessor contextAccessor)
        {
            _mediator = mediator;
            _contextAccessor = contextAccessor;
            _user = _contextAccessor.HttpContext!.User.GetUser();
        }
        
        // TODO: Add endpoints for:
        // - GET all rooms with their current status
        // - PUT room status (Vacant, Occupied, Dirty, Maintenance)
        // - GET rooms by floor
        // - POST mark room for maintenance
    }
}
