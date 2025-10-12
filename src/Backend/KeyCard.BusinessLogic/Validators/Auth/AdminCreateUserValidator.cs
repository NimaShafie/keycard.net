using FluentValidation;

using KeyCard.BusinessLogic.Commands.Auth;

namespace KeyCard.BusinessLogic.Validators.Auth
{
    public class AdminCreateUserCommandValidator : AbstractValidator<AdminCreateUserCommand>
    {
        public AdminCreateUserCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().MaximumLength(100);

            RuleFor(x => x.FirstName)
                .NotEmpty().MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty().MinimumLength(6);

            RuleFor(x => x.Role)
                .NotEmpty()
                .Must(r => new[] { "Guest", "Employee", "HouseKeeping" }.Contains(r))
                .WithMessage("Role must be Guest, Employee, or HouseKeeping.");

            // Guests need address and country
            When(x => x.Role == "Guest", () =>
            {
                RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required for guests.");
                RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required for guests.");
            });
        }
    }
}
