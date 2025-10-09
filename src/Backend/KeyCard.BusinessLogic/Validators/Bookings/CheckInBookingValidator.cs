using FluentValidation;
using KeyCard.BusinessLogic.Commands.Bookings;

namespace KeyCard.BusinessLogic.Validators.Bookings
{
    public class CheckInBookingValidator : AbstractValidator<CheckInBookingCommand>
    {
        public CheckInBookingValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0)
                .WithMessage("Invalid Booking ID.");
        }
    }
}
