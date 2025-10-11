using KeyCard.BusinessLogic.Commands.Bookings;
using KeyCard.BusinessLogic.ViewModels.Auth;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Auth
{
    public record GuestSignupCommand(
        string Email,
        string FullName,
        string Password
    ) : Request, IRequest<AuthResultViewModel>;

    //public async Task<AuthResultViewModel> Handle(GuestSignupCommand command, CancellationToken cancellationToken)
    //{
        //return await _bookingService.CancelBookingAsync(command, cancellationToken);
    //}
}
