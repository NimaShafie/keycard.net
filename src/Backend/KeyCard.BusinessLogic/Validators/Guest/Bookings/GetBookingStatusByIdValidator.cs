using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Bookings;

namespace KeyCard.BusinessLogic.Validators.Guest.Bookings
{
    public class GetBookingStatusByIdValidator : AbstractValidator<GetBookingByIdCommand>
    {
        public GetBookingStatusByIdValidator()
        {
            RuleFor(x => x.BookingId)
                .NotEmpty()
                .WithMessage("Booking ID must be a positive integer.");
        }
    }
}
