using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Tasks;
using KeyCard.Core.Common;


namespace KeyCard.BusinessLogic.Validators.Tasks
{
    public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
    {
        public UpdateTaskCommandValidator()
        {
            // Task ID must be valid (positive integer)
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than zero.");

            // Task name is required and limited in length
            RuleFor(x => x.TaskName)
                .NotEmpty()
                .WithMessage("Task name is required.")
                .MaximumLength(100)
                .WithMessage("Task name cannot exceed 100 characters.");

            // Notes are optional but limited
            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.");

            // AssignedToId is optional, but must be a valid GUID if provided
            RuleFor(x => x.AssignedToId)
                .GreaterThan(0)
                .When(x => x.AssignedToId.HasValue)
                .WithMessage("AssignedToId must be greater than zero if provided.");

            // Status validation — must match one of the defined TaskStatusEnum values
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(BeAValidStatus)
                .WithMessage("Invalid task status value.");
        }

        private bool BeAValidStatus(string status)
        {
            return Enum.TryParse(typeof(TaskStatusEnum), status, true, out _);
        }
    }
}
