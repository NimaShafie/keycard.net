using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Tasks
{
    public record CreateTaskCommand(string TaskName, string? Notes, int RoomId, Guid? AssignedToId)
        : IRequest<TaskDto>;

    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
    {
        private readonly ITaskService _taskService;

        public CreateTaskCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<TaskDto> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.CreateTaskAsync(command, cancellationToken);
        }
    }
}
