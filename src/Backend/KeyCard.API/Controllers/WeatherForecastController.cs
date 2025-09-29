using KeyCard.BusinessLogic.Commands;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace KeyCard.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WeatherForecastController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("say")]
        public async Task<IActionResult> Say([FromBody] DemoCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new { Message = result });
        }
    }
}
