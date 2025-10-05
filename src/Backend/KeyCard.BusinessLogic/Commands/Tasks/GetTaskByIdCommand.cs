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
    public record GetTaskByIdCommand (int Id) : IRequest<TaskDto>;

    public class GetTaskByIdCommandHandler : IRequestHandler<GetTaskByIdCommand, TaskDto>
    {
        private readonly ITaskService _taskService;

        public GetTaskByIdCommandHandler(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<TaskDto> Handle(GetTaskByIdCommand command, CancellationToken cancellationToken)
        {
            return await _taskService.GetTaskByIdAsync(command, cancellationToken);
        }
    }
}
