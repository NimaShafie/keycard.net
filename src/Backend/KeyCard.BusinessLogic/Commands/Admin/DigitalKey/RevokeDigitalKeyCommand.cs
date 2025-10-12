using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Admin.DigitalKey
{
    public record RevokeDigitalKeyCommand (int BookingId) : Request, IRequest<bool>;

    public class RevokeDigitalKeyCommandHandler : IRequestHandler<RevokeDigitalKeyCommand, bool>
    {
        private readonly IDigitalKeyService _digitalKeyService;

        public RevokeDigitalKeyCommandHandler(IDigitalKeyService digitalKeyService)
        {
            _digitalKeyService = digitalKeyService;
        }

        public async Task<bool> Handle(RevokeDigitalKeyCommand command, CancellationToken cancellationToken)
        {
            return await _digitalKeyService.RevokeKeyAsync(command, cancellationToken);
        }
    }
}
