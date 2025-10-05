using KeyCard.BusinessLogic.Commands.Tasks;
using KeyCard.BusinessLogic.ViewModels;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "Admin,HouseKeeping,Employee")]
    public class HousekeepingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HousekeepingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<TaskDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllTasksCommand());
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateTaskCommand command)
        {
            if (id != command.Id) return BadRequest("Task ID mismatch.");
            var success = await _mediator.Send(command);
            return success ? Ok("Task updated successfully.") : NotFound("Task not found.");
        }

        [HttpPost("{id:int}/complete")]
        public async Task<ActionResult> Complete(int id)
        {
            var success = await _mediator.Send(new CompleteTaskCommand(id));
            return success ? Ok("Task completed.") : NotFound("Task not found.");
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _mediator.Send(new DeleteTaskCommand(id));
            return success ? Ok("Task deleted.") : NotFound("Task not found.");
        }
    }
}
