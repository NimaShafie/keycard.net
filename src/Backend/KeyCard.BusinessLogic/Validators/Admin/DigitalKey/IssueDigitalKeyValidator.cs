using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.DigitalKey;

namespace KeyCard.BusinessLogic.Validators.Admin.DigitalKey
{
    public class IssueDigitalKeyValidator : AbstractValidator<IssueDigitalKeyCommand>
    {
        public IssueDigitalKeyValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0)
                .WithMessage("BookingId must be a positive integer.");
        }
    }
}
