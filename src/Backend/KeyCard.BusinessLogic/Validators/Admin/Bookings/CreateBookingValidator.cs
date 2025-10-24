using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Bookings;
using KeyCard.Core.Common;

namespace KeyCard.BusinessLogic.Validators.Admin.Bookings
{
    public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
    {
        public CreateBookingValidator()
        {
            // If both dates are supplied, ensure logical range
            RuleFor(x => x.CheckInDate)
                .NotNull().NotEmpty().GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Invalid CheckIn Date.");

            RuleFor(x => x.CheckOutDate)
                .NotNull().NotEmpty()
                .WithMessage("Invalid Checkout Date.");

            RuleFor(x => x.CheckInDate)
                .GreaterThanOrEqualTo(x => x.CheckOutDate)
                .WithMessage("Checkout date must be greater than or equal to CheckIn Date.");

            RuleFor(x => x.Adults)
                .NotNull().GreaterThan(0)
                .WithMessage("There should be atleast one adult for booking.");

            RuleFor(x => x.RoomId)
                .NotNull().GreaterThan(0)
                .WithMessage("Invalid Room Id.");

            RuleFor(x => x.GuestProfileId)
                .NotNull().GreaterThan(0)
                .WithMessage("Invalid Guest Id.");
        }
    }
}
