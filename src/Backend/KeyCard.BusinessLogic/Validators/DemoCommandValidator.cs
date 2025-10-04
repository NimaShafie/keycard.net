using FluentValidation;

using KeyCard.BusinessLogic.Commands;

namespace KeyCard.BusinessLogic.Validators
{
    public class DemoCommandValidator : AbstractValidator<DemoCommand>
    {
        public DemoCommandValidator()
        {
            RuleFor(x => x.s)
                .NotEmpty().WithMessage("Value cannot be empty")
                .MinimumLength(3).WithMessage("Value must be at least 3 characters long");
        }
    }
}
