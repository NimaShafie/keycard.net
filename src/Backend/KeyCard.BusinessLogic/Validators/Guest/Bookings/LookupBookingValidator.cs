using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using KeyCard.BusinessLogic.Commands.Guest.Bookings;

namespace KeyCard.BusinessLogic.Validators.Guest.Bookings
{
    public class LookupBookingValidator : AbstractValidator<LookupBookingCommand>
    {
        public LookupBookingValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Booking code is required.")
                .MaximumLength(20).WithMessage("Booking code must not exceed 20 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");
        }
    }
}
