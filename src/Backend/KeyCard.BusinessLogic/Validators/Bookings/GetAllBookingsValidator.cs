using FluentValidation;

using KeyCard.BusinessLogic.Commands.Bookings;
using KeyCard.Core.Common;

namespace KeyCard.BusinessLogic.Validators.Bookings
{
    public class GetAllBookingsValidator : AbstractValidator<GetAllBookingsCommand>
    {
        public GetAllBookingsValidator()
        {
            // If both dates are supplied, ensure logical range
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                .WithMessage("ToDate must be greater than or equal to FromDate.");

            // Optional: validate status string
            RuleFor(x => x.Status)
                .Must(status => string.IsNullOrWhiteSpace(status) ||
                                Enum.TryParse(typeof(BookingStatus), status, true, out _))
                .WithMessage("Invalid booking status.");

            // Optional: guest name length
            RuleFor(x => x.GuestName)
                .MaximumLength(100)
                .WithMessage("Guest name filter is too long.");
        }
    }
}
