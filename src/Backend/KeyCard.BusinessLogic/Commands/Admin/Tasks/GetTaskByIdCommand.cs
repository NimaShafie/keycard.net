using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;
using KeyCard.BusinessLogic.ViewModels.Task;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Admin.Tasks
{
    public record GetTaskByIdCommand (int Id) : Request, IRequest<TaskViewModel>;

    public class GetTaskByIdCommandHandler : IRequestHandler<GetTaskByIdCommand, TaskViewModel>
    {
        private readonly ITaskService _taskService;

        public GetTaskByIdCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<TaskViewModel> Handle(GetTaskByIdCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.GetTaskByIdAsync(command, cancellationToken);
        }
    }
}
