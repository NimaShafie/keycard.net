using KeyCard.BusinessLogic.Commands.Bookings;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.DigitalKey
{
    public record IssueDigitalKeyCommand (int BookingId) : Request, IRequest<DigitalKeyViewModel>;

    public class IssueDigitalKeyCommandHandler : IRequestHandler<IssueDigitalKeyCommand, DigitalKeyViewModel>
    {
        private readonly IDigitalKeyService _digitalKeyService;

        public IssueDigitalKeyCommandHandler(IDigitalKeyService digitalKeyService)
        {
            _digitalKeyService = digitalKeyService;
        }

        public async Task<DigitalKeyViewModel> Handle(IssueDigitalKeyCommand command, CancellationToken cancellationToken)
        {
            return await _digitalKeyService.IssueKeyAsync(command, cancellationToken);
        }
    }
}
