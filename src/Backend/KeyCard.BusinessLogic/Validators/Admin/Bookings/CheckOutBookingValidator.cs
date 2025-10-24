using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Bookings;

namespace KeyCard.BusinessLogic.Validators.Admin.Bookings
{
    public class CheckOutBookingValidator : AbstractValidator<CheckOutBookingCommand>
    {
        public CheckOutBookingValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0)
                .WithMessage("Invalid booking ID.");
        }
    }
}
