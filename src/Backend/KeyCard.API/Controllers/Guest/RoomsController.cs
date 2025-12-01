// ============================================================================
// GUEST ROOMS CONTROLLER - ROOM SEARCH FOR BOOKING
// when guest want to book, they first need to see whats available
// this shows room types, prices, amenities, availability
// ============================================================================

using KeyCard.BusinessLogic.Commands.Guest.Rooms;
using KeyCard.BusinessLogic.ViewModels.Rooms;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Guest
{
    /// <summary>
    /// Public room search - no login needed!
    /// Guest browses available rooms before making reservation
    /// Shows prices, photos, amenities - everything to help them decide
    /// </summary>
    [ApiController]
    [Route("api/guest/[controller]")]
    public class RoomsController : Controller
    {
        private readonly IMediator _mediator;

        public RoomsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Search available rooms for given dates and guest count
        /// Returns all room types that can fit your party
        /// Prices calculated based on number of nights
        /// </summary>
        /// <param name="checkIn">When you arrive</param>
        /// <param name="checkOut">When you leave</param>
        /// <param name="guests">How many people total</param>
        /// <param name="rooms">How many rooms you need</param>
        /// <param name="currency">USD, EUR, etc. (not fully implemented yet)</param>
        [HttpGet("room-options")]
        [ProducesResponseType(typeof(RoomOptionsViewModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoomOptions(
            [FromQuery] DateOnly checkIn,
            [FromQuery] DateOnly checkOut,
            [FromQuery] int guests = 1,
            [FromQuery] int rooms = 1,
            [FromQuery] string currency = "USD")
        {
            // service checks room capacity, availability, calculates prices
            var vm = await _mediator.Send(new GetRoomOptionsCommand(
                checkIn, checkOut, guests, rooms, currency));

            return Ok(vm);
        }
    }
}
