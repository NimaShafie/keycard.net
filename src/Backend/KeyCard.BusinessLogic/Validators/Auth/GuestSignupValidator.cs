using FluentValidation;
using KeyCard.BusinessLogic.Commands.Auth;

namespace KeyCard.BusinessLogic.Validators.Auth
{
    public class GuestSignupCommandValidator : AbstractValidator<GuestSignupCommand>
    {
        public GuestSignupCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters long.");
        }
    }
}
