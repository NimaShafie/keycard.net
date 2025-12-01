// ============================================================================
// HOUSEKEEPING CONTROLLER - CLEANING TASKS MANAGEMENT
// keeping rooms sparkling clean is very important!
// this controller manages cleaning tasks for housekeeping staff
// ============================================================================

using KeyCard.Api.Helper;
using KeyCard.BusinessLogic.Commands.Admin.Tasks;
using KeyCard.BusinessLogic.ViewModels.Task;
using KeyCard.BusinessLogic.ViewModels.UserClaims;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers.Admin
{
    /// <summary>
    /// Housekeeping task management
    /// Staff can create, update, complete, and delete cleaning tasks
    /// Tasks are auto-created when guests check out (see BookingService)
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
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

        /// <summary>
        /// Get all housekeeping tasks - shows what rooms need cleaning
        /// Desktop app displays this in task dashboard
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<TaskViewModel>>> GetAll()
        {
            var command = new GetAllTasksCommand();
            command.User = _user;

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Create new cleaning task manually
        /// Sometimes guest spills coffee and needs extra cleaning :)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TaskViewModel>> Create([FromBody] CreateTaskCommand command)
        {
            command.User = _user;
            var result = await _mediator.Send(command);
            // 201 Created with location header - proper REST!
            return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
        }

        /// <summary>
        /// Update task details - reassign to different staff, change notes, etc.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateTaskCommand command)
        {
            command.User = _user;

            // sanity check - URL id must match body id
            if (id != command.Id) return BadRequest("Task ID mismatch.");
            
            var success = await _mediator.Send(command);
            return success ? Ok("Task updated successfully.") : NotFound("Task not found.");
        }

        /// <summary>
        /// Mark task as completed - room is clean now!
        /// Housekeeping staff clicks "Done" button on their device
        /// </summary>
        [HttpPost("{id:int}/complete")]
        public async Task<ActionResult> Complete(int id)
        {
            var command = new CompleteTaskCommand(id) { User = _user };
            var success = await _mediator.Send(command);
            return success ? Ok("Task completed.") : NotFound("Task not found.");
        }

        /// <summary>
        /// Delete a task - soft delete actually, we keep audit trail
        /// Maybe task was created by mistake
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var command = new DeleteTaskCommand(id) { User = _user };
            var success = await _mediator.Send(command);
            return success ? Ok("Task deleted.") : NotFound("Task not found.");
        }
    }
}
