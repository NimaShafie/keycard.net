using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;
using MediatR;

namespace KeyCard.BusinessLogic.Commands.Tasks
{
    public record UpdateTaskCommand(int Id, string TaskName, string? Notes, int? AssignedToId, string Status)
        : Request, IRequest<bool>;

    public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, bool>
    {
        private readonly ITaskService _taskService;

        public UpdateTaskCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<bool> Handle(UpdateTaskCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.UpdateTaskAsync(command, cancellationToken);
        }
    }
}
