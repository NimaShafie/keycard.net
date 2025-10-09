using KeyCard.BusinessLogic.Commands.DigitalKey;
using KeyCard.BusinessLogic.ViewModels.Booking;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface IDigitalKeyService
    {
        Task<DigitalKeyViewModel> IssueKeyAsync(IssueDigitalKeyCommand command, CancellationToken cancellationToken);
        Task<bool> RevokeKeyAsync(RevokeDigitalKeyCommand command, CancellationToken cancellationToken);
    }
}

