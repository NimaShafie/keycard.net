using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Bookings;

namespace KeyCard.BusinessLogic.Validators.Admin.Bookings
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
