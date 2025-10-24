using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Tasks;

namespace KeyCard.BusinessLogic.Validators.Admin.Tasks
{
    public class GetTaskByIdValidator : AbstractValidator<GetTaskByIdCommand>
    {
        public GetTaskByIdValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("TaskId must be a positive integer.");
        }
    }
}
