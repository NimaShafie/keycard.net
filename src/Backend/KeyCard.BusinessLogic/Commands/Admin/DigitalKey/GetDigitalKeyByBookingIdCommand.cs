using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Admin.DigitalKey
{
    public record GetDigitalKeyByBookingIdCommand(int BookingId) : Request, IRequest<DigitalKeyViewModel>;

    public class GetDigitalKeyByBookingIdCommandHandler : IRequestHandler<GetDigitalKeyByBookingIdCommand, DigitalKeyViewModel>
    {
        private readonly IDigitalKeyService _digitalKeyService;

        public GetDigitalKeyByBookingIdCommandHandler(IDigitalKeyService digitalKeyService)
        {
            _digitalKeyService = digitalKeyService;
        }

        public async Task<DigitalKeyViewModel> Handle(GetDigitalKeyByBookingIdCommand command, CancellationToken cancellationToken)
        {
            return await _digitalKeyService.GetDigitalKeyByBookingIdAsync(command, cancellationToken);
        }
    }
}
