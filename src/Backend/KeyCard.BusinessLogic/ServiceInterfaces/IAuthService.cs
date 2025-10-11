using KeyCard.BusinessLogic.Commands.Auth;
using KeyCard.BusinessLogic.ViewModels.Auth;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface IAuthService
    {
        Task<AuthResultViewModel> GuestSignupAsync(GuestSignupCommand command, CancellationToken cancellationToken);
    }
}
