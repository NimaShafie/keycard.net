using FluentValidation;

using KeyCard.BusinessLogic.Commands.Bookings;

namespace KeyCard.BusinessLogic.Validators.Bookings
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
