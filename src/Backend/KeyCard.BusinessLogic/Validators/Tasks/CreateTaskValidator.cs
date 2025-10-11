using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using KeyCard.BusinessLogic.Commands.Tasks;

namespace KeyCard.BusinessLogic.Validators.Tasks
{
    public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskValidator()
        {
            // Task name is always required
            RuleFor(x => x.TaskName)
                .NotEmpty()
                .WithMessage("Task name is required.")
                .MaximumLength(100)
                .WithMessage("Task name cannot exceed 100 characters.");

            // Notes are optional but limited in size
            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.");

            // RoomId can be null (for general tasks), but if present, must be > 0
            RuleFor(x => x.RoomId)
                .GreaterThan(0)
                .When(x => x.RoomId.HasValue)
                .WithMessage("RoomId must be greater than zero if provided.");

            // AssignedToId is optional, but must be a valid Guid if provided
            RuleFor(x => x.AssignedToId)
                .GreaterThan(0)
                .WithMessage("AssignedToId must be greater than zero.");
        }
    }
}
