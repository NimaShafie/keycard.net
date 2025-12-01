// ============================================================================
// TASK SERVICE - HOUSEKEEPING OPERATIONS
// keeping rooms clean is serious business!
// manages cleaning tasks, assigns to staff, tracks completion
// ============================================================================

using KeyCard.BusinessLogic.Commands.Admin.Tasks;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Task;
using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.AppDbContext;
using KeyCard.Infrastructure.Models.HouseKeeping;

using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    /// <summary>
    /// Housekeeping task service
    /// Tasks are created automatically when guests check out
    /// Staff can also create tasks manually (e.g., "fix broken lamp in room 305")
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly ApplicationDBContext _context;

        public TaskService(ApplicationDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all housekeeping tasks - the daily to-do list for cleaning staff
        /// Shows room number and who its assigned to
        /// </summary>
        public async Task<List<TaskViewModel>> GetAllTasksAsync(GetAllTasksCommand command, CancellationToken cancellationToken)
        {
            return await _context.HousekeepingTasks
                .Where(t => !t.IsDeleted)  // skip deleted tasks
                .Include(t => t.Room)
                .Include(t => t.AssignedTo)
                .Select(t => new TaskViewModel(
                    t.Id,
                    t.TaskName,
                    t.Notes,
                    t.Status.ToString(),
                    t.Room != null ? t.Room.RoomNumber : null,  // some tasks might not be room-specific
                    t.AssignedTo != null ? t.AssignedTo.FullName : null))  // might be unassigned
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get single task by ID - staff clicks on a task to see details
        /// </summary>
        public async Task<TaskViewModel> GetTaskByIdAsync(GetTaskByIdCommand command, CancellationToken cancellationToken)
        {
            var task = await _context.HousekeepingTasks
                .Include(t => t.Room)
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == command.Id && !t.IsDeleted, cancellationToken);

            if (task == null)
            {
                throw new KeyNotFoundException($"Task with ID {command.Id} not found.");
            }

            return new TaskViewModel(task.Id, task.TaskName, task.Notes, task.Status.ToString(),
                task!.Room?.RoomNumber, task.AssignedTo?.FullName);
        }

        /// <summary>
        /// Create a new task manually
        /// Maybe guest reported something broken, or supervisor wants deep cleaning
        /// </summary>
        public async Task<TaskViewModel> CreateTaskAsync(CreateTaskCommand command, CancellationToken cancellationToken)
        {
            var task = new HousekeepingTask
            {
                TaskName = command.TaskName,
                Notes = command.Notes,
                RoomId = command.RoomId,
                AssignedToId = command.AssignedToId,  // can be null if not assigned yet
                Status = TaskStatusEnum.Pending,       // all new tasks start as pending
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.User!.UserId
            };  

            await _context.HousekeepingTasks.AddAsync(task);
            await _context.SaveChangesAsync(cancellationToken);

            // get room number for the response
            var room = await _context.Rooms.FindAsync(task.RoomId);
            return new TaskViewModel(task.Id, task.TaskName, task.Notes, task.Status.ToString(),
                room?.RoomNumber ?? "N/A", null);
        }

        /// <summary>
        /// Update task - change assignment, add notes, change status
        /// Supervisor might reassign task to different staff member
        /// </summary>
        public async Task<bool> UpdateTaskAsync(UpdateTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _context.HousekeepingTasks
                .FirstOrDefaultAsync(t => t.Id == command.Id && !t.IsDeleted, cancellationToken);

            if (task == null)
            {
                throw new KeyNotFoundException($"Task with ID {command.Id} not found.");
            }

            // update all the fields
            task.TaskName = command.TaskName;
            task.Notes = command.Notes;
            task.AssignedToId = command.AssignedToId;

            // update status if provided (Pending, InProgress, Completed)
            if (Enum.TryParse<TaskStatusEnum>(command.Status, true, out var status))
                task.UpdateStatus(status);

            // audit trail
            task.LastUpdatedAt = DateTime.UtcNow;
            task.LastUpdatedBy = command.User!.UserId;

            _context.HousekeepingTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <summary>
        /// Mark task as completed - room is clean!
        /// Staff clicks "Done" button on their device after cleaning
        /// </summary>
        public async Task<bool> CompleteTaskAsync(CompleteTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _context.HousekeepingTasks
                .FirstOrDefaultAsync(t => t.Id == command.Id && !t.IsDeleted, cancellationToken);

            if (task == null)
            {
                throw new KeyNotFoundException($"Task with ID {command.Id} not found.");
            }

            // already done? no problem, just return success
            // prevents double-completion errors
            if (task.Status == TaskStatusEnum.Completed)
                return true;

            task.UpdateStatus(TaskStatusEnum.Completed);
            task.CompletedAt = DateTime.UtcNow;  // record when it was finished
            task.LastUpdatedAt = DateTime.UtcNow;
            task.LastUpdatedBy = command.User!.UserId;

            _context.HousekeepingTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            // TODO: could auto-update room status to Vacant here

            return true;
        }

        /// <summary>
        /// Delete a task - soft delete, we keep it for audit purposes
        /// Maybe task was created by mistake or is no longer needed
        /// </summary>
        public async Task<bool> DeleteTaskAsync(DeleteTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _context.HousekeepingTasks.FindAsync(command.Id);

            if (task == null || task.IsDeleted)
            {
                throw new KeyNotFoundException($"Task with ID {command.Id} not found.");
            }

            // soft delete - just mark as deleted, dont actually remove
            // this way we can still see it in reports if needed
            task.IsDeleted = true;
            task.LastUpdatedAt = DateTime.UtcNow;
            task.LastUpdatedBy = command.User!.UserId;
            
            _context.HousekeepingTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
