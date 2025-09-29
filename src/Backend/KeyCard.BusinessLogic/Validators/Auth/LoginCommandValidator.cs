using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using KeyCard.BusinessLogic.Commands.Auth;

namespace KeyCard.BusinessLogic.Validators.Auth
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator() {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username cannot be empty")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Username cannot be empty");
        }
    }
}
