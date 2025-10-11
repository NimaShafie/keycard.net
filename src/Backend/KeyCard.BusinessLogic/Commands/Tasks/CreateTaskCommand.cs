using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;
using KeyCard.BusinessLogic.ViewModels.Task;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Tasks
{
    public record CreateTaskCommand(string TaskName, string? Notes, int? RoomId, int? AssignedToId)
        : Request, IRequest<TaskViewModel>;

    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskViewModel>
    {
        private readonly ITaskService _taskService;

        public CreateTaskCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<TaskViewModel> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.CreateTaskAsync(command, cancellationToken);
        }
    }
}
