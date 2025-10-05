using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;
using MediatR;

namespace KeyCard.BusinessLogic.Commands.Tasks
{
    public record GetAllTasksCommand() : IRequest<List<TaskDto>>;

    public class GetAllTasksCommandHandler : IRequestHandler<GetAllTasksCommand, List<TaskDto>>
    {
        private readonly ITaskService _taskService;

        public GetAllTasksCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<List<TaskDto>> Handle(GetAllTasksCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.GetAllTasksAsync(command, cancellationToken);
        }
    }
}
