using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Auth;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Auth
{
    public record GuestSignupCommand(
        string Email,
        string FirstName,
        string? LastName,
        string Password
    ) : Request, IRequest<AuthResultViewModel>;

    public class GuestSignupCommandHandler : IRequestHandler<GuestSignupCommand, AuthResultViewModel>
    {
        private readonly IAuthService _authService;

        public GuestSignupCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<AuthResultViewModel> Handle(GuestSignupCommand command, CancellationToken cancellationToken)
        {
            return await _authService.GuestSignupAsync(command, cancellationToken);
        }
    }
}
