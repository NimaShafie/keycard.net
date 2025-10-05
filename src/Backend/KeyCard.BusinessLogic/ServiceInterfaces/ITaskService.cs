using KeyCard.BusinessLogic.Commands.Tasks;
using KeyCard.BusinessLogic.ViewModels;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface ITaskService
    {
        Task<List<TaskDto>> GetAllTasksAsync(GetAllTasksCommand command, CancellationToken cancellationToken);
        Task<TaskDto?> GetTaskByIdAsync(GetTaskByIdCommand command, CancellationToken cancellationToken);
        Task<TaskDto> CreateTaskAsync(CreateTaskCommand command, CancellationToken cancellationToken);
        Task<bool> UpdateTaskAsync(UpdateTaskCommand command, CancellationToken cancellationToken);
        Task<bool> CompleteTaskAsync(CompleteTaskCommand command, CancellationToken cancellationToken);
        Task<bool> DeleteTaskAsync(DeleteTaskCommand command, CancellationToken cancellationToken);
    }
}
