using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Bookings;
using KeyCard.BusinessLogic.Commands.Guest.Bookings;

namespace KeyCard.BusinessLogic.Validators.Guest.Bookings
{
    public class GetMyBookingsValidator : AbstractValidator<GetMyBookingsCommand>
    {
        public GetMyBookingsValidator()
        {
            RuleFor(x => x.GuestId)
                .NotEmpty()
                .WithMessage("Guest ID is required for cancellation.");
        }
    }
}
