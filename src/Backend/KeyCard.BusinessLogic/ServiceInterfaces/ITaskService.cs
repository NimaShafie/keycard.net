using KeyCard.BusinessLogic.Commands.Tasks;
using KeyCard.BusinessLogic.ViewModels.Task;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface ITaskService
    {
        Task<List<TaskViewModel>> GetAllTasksAsync(GetAllTasksCommand command, CancellationToken cancellationToken);
        Task<TaskViewModel?> GetTaskByIdAsync(GetTaskByIdCommand command, CancellationToken cancellationToken);
        Task<TaskViewModel> CreateTaskAsync(CreateTaskCommand command, CancellationToken cancellationToken);
        Task<bool> UpdateTaskAsync(UpdateTaskCommand command, CancellationToken cancellationToken);
        Task<bool> CompleteTaskAsync(CompleteTaskCommand command, CancellationToken cancellationToken);
        Task<bool> DeleteTaskAsync(DeleteTaskCommand command, CancellationToken cancellationToken);
    }
}
