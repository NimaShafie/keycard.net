using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Auth;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Auth
{
    public record AdminCreateUserCommand(
        string FirstName,
        string? LastName,
        string Email,
        string Password,
        string Role,
        string? Address,
        string? Country
    ) : Request, IRequest<AuthResultViewModel>;

    public class AdminCreateUserCommandHandler : IRequestHandler<AdminCreateUserCommand, AuthResultViewModel>
    {
        private readonly IAuthService _authService;

        public AdminCreateUserCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<AuthResultViewModel> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
        {
            return await _authService.AdminCreateUserAsync(request, cancellationToken);
        }
    }
}
