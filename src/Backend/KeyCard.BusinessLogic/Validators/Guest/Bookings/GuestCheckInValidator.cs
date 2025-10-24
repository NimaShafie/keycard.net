using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using KeyCard.BusinessLogic.Commands.Guest.Bookings;

namespace KeyCard.BusinessLogic.Validators.Guest.Bookings
{
    public class GuestCheckInValidator : AbstractValidator<GuestCheckInCommand>
    {
        public GuestCheckInValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0).WithMessage("BookingId must be greater than 0.");

            RuleFor(x => x.GuestId)
                .GreaterThan(0).WithMessage("GuestId must be greater than 0.");
        }
    }
}
