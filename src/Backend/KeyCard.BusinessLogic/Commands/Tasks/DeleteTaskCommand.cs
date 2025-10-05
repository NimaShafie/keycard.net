using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Tasks
{
    public record DeleteTaskCommand(int Id) : IRequest<bool>;

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
