using KeyCard.BusinessLogic.Commands.Tasks;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Task;
using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.AppDbContext;
using KeyCard.Infrastructure.Models.HouseKeeping;

using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDBContext _context;

        public TaskService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<TaskViewModel>> GetAllTasksAsync(GetAllTasksCommand command, CancellationToken cancellationToken)
        {
            return await _context.HousekeepingTasks
                .Where(t => !t.IsDeleted)
                .Include(t => t.Room)
                .Include(t => t.AssignedTo)
                .Select(t => new TaskViewModel(
                    t.Id,
                    t.TaskName,
                    t.Notes,
                    t.Status.ToString(),
                    t.Room.RoomNumber,
                    t.AssignedTo != null ? t.AssignedTo.FullName : null))
                .ToListAsync(cancellationToken);
        }

        public async Task<TaskViewModel?> GetTaskByIdAsync(GetTaskByIdCommand command, CancellationToken cancellationToken)
        {
            var task = await _context.HousekeepingTasks
                .Include(t => t.Room)
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == command.Id && !t.IsDeleted, cancellationToken);

            if (task == null) return null;

            return new TaskViewModel(task.Id, task.TaskName, task.Notes, task.Status.ToString(),
                task.Room.RoomNumber, task.AssignedTo?.FullName);
        }

        public async Task<TaskViewModel> CreateTaskAsync(CreateTaskCommand command, CancellationToken cancellationToken)
        {
            var task = new HousekeepingTask
            {
                TaskName = command.TaskName,
                Notes = command.Notes,
                RoomId = command.RoomId,
                AssignedToId = command.AssignedToId,
                Status = TaskStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = new Guid("system")
            };  

            _context.HousekeepingTasks.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            var room = await _context.Rooms.FindAsync(task.RoomId);
            return new TaskViewModel(task.Id, task.TaskName, task.Notes, task.Status.ToString(),
                room?.RoomNumber ?? "N/A", null);
        }

        public async Task<bool> UpdateTaskAsync(UpdateTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _context.HousekeepingTasks
                .FirstOrDefaultAsync(t => t.Id == command.Id && !t.IsDeleted, cancellationToken);

            if (task == null)
                return false; // Not found or soft deleted

            task.TaskName = command.TaskName;
            task.Notes = command.Notes;
            task.AssignedToId = command.AssignedToId;

            if (Enum.TryParse<TaskStatusEnum>(command.Status, true, out var status))
                task.Status = status;

            task.LastUpdatedAt = DateTime.UtcNow;
            task.LastUpdatedBy = new Guid("system"); // Replace with current user if available

            _context.HousekeepingTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }


        public async Task<bool> CompleteTaskAsync(CompleteTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _context.HousekeepingTasks
                .FirstOrDefaultAsync(t => t.Id == command.Id && !t.IsDeleted, cancellationToken);

            if (task == null)
                return false; // Task not found or deleted

            if (task.Status == TaskStatusEnum.Completed)
                return true; // Already completed — safe no-op

            task.Status = TaskStatusEnum.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.LastUpdatedAt = DateTime.UtcNow;
            task.LastUpdatedBy = new Guid("system");

            _context.HousekeepingTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> DeleteTaskAsync(DeleteTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _context.HousekeepingTasks.FindAsync(command.Id);
            if (task == null || task.IsDeleted)
                return false;

            task.IsDeleted = true;
            task.LastUpdatedAt = DateTime.UtcNow;
            task.LastUpdatedBy = new Guid("system"); // or current user
            _context.HousekeepingTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
