using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Bookings;

namespace KeyCard.BusinessLogic.Validators.Bookings
{
    public class CancelBookingValidator : AbstractValidator<CancelBookingCommand>
    {
        public CancelBookingValidator()
        {
            RuleFor(x => x.BookingId)
                .NotEmpty()
                .WithMessage("Booking ID is required for cancellation.");
        }
    }
}
