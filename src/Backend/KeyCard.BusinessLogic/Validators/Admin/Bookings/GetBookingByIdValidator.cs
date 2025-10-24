using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Bookings;
using KeyCard.Core.Common;

namespace KeyCard.BusinessLogic.Validators.Admin.Bookings
{
    public class GetBookingByIdValidator : AbstractValidator<GetBookingByIdCommand>
    {
        public GetBookingByIdValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0)
                .WithMessage("BookingId must be a positive integer.");
        }
    }
}
