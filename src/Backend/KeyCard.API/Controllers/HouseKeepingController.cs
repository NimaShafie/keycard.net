using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.Commands.Tasks;
using KeyCard.BusinessLogic.ViewModels.Task;
using KeyCard.BusinessLogic.ViewModels.UserClaims;

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
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserClaimsViewModel _user;

        public HousekeepingController(IMediator mediator, IHttpContextAccessor contextAccessor)
        {
            _mediator = mediator;
            _contextAccessor = contextAccessor;
            _user = _contextAccessor.HttpContext!.User.GetUser();
        }

        [HttpGet]
        public async Task<ActionResult<List<TaskViewModel>>> GetAll()
        {
            GetAllTasksCommand command = new GetAllTasksCommand();
            command.User = this._user;

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<TaskViewModel>> Create([FromBody] CreateTaskCommand command)
        {
            command.User = this._user;
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateTaskCommand command)
        {
            command.User = this._user;

            if (id != command.Id) return BadRequest("Task ID mismatch.");
            var success = await _mediator.Send(command);
            return success ? Ok("Task updated successfully.") : NotFound("Task not found.");
        }

        [HttpPost("{id:int}/complete")]
        public async Task<ActionResult> Complete(int id)
        {
            var command = new CompleteTaskCommand(id) { User = this._user };
            var success = await _mediator.Send(command);
            return success ? Ok("Task completed.") : NotFound("Task not found.");
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var command = new DeleteTaskCommand(id) { User = this._user };
            var success = await _mediator.Send(command);
            return success ? Ok("Task deleted.") : NotFound("Task not found.");
        }
    }
}
