using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;
using MediatR;

namespace KeyCard.BusinessLogic.Commands.Tasks
{
    public record DeleteTaskCommand(int Id) : Request, IRequest<bool>;

    public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, bool>
    {
        private readonly ITaskService _taskService;

        public DeleteTaskCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<bool> Handle(DeleteTaskCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.DeleteTaskAsync(command, cancellationToken);
        }
    }
}
