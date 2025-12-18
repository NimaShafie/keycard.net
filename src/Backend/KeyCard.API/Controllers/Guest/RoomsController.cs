using KeyCard.BusinessLogic.Commands.Guest.Rooms;
using KeyCard.BusinessLogic.ViewModels.Rooms;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Guest
{
    [ApiController]
    [Route("api/guest/[controller]")]
    public class RoomsController : Controller
    {
        private readonly IMediator _mediator;

        public RoomsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("options")]
        [ProducesResponseType(typeof(RoomOptionsViewModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoomOptions(
            [FromQuery] DateOnly checkIn,
            [FromQuery] DateOnly checkOut,
            [FromQuery] int guests = 1,
            [FromQuery] int rooms = 1,
            [FromQuery] string currency = "USD")
        {
            var vm = await _mediator.Send(new GetRoomOptionsCommand(
                checkIn, checkOut, guests, rooms, currency));

            return Ok(vm);
        }
    }
}
