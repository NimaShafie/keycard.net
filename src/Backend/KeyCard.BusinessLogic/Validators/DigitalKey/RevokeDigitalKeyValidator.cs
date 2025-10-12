using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.DigitalKey;

namespace KeyCard.BusinessLogic.Validators.Keys
{
    public class RevokeDigitalKeyValidator : AbstractValidator<RevokeDigitalKeyCommand>
    {
        public RevokeDigitalKeyValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0)
                .WithMessage("BookingId must be a positive integer.");
        }
    }
}
