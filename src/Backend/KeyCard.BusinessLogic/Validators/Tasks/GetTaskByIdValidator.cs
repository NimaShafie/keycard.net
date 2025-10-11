using FluentValidation;
using KeyCard.BusinessLogic.Commands.Tasks;

namespace KeyCard.BusinessLogic.Validators.Tasks
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
