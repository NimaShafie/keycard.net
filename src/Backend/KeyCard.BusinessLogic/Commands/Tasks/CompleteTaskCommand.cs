using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;
using MediatR;

namespace KeyCard.BusinessLogic.Commands.Tasks
{
    public record CompleteTaskCommand(int Id) : Request, IRequest<bool>;

    public class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand, bool>
    {
        private readonly ITaskService _taskService;

        public CompleteTaskCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<bool> Handle(CompleteTaskCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.CompleteTaskAsync(command, cancellationToken);
        }
    }
}
