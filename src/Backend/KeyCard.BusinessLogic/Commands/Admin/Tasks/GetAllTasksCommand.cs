using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;
using KeyCard.BusinessLogic.ViewModels.Task;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Admin.Tasks
{
    public record GetAllTasksCommand() : Request, IRequest<List<TaskViewModel>>;

    public class GetAllTasksCommandHandler : IRequestHandler<GetAllTasksCommand, List<TaskViewModel>>
    {
        private readonly ITaskService _taskService;

        public GetAllTasksCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<List<TaskViewModel>> Handle(GetAllTasksCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.GetAllTasksAsync(command, cancellationToken);
        }
    }
}
