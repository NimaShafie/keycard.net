using System.Security.Claims;

using KeyCard.BusinessLogic.ViewModels.UserClaims;

namespace KeyCard.Api.Helper
{
    public static class ClaimsPrincipalExtensions
        {
            public static int GetUserId(this ClaimsPrincipal user)
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (idClaim == null)
                    throw new UnauthorizedAccessException("User ID claim not found.");

                return int.Parse(idClaim);
            }

            public static string GetUserEmail(this ClaimsPrincipal user)
            {
                return user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            }

            public static string GetUserRole(this ClaimsPrincipal user)
            {
                return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            }

            // Optional: wrap into a mini DTO
            public static UserClaimsViewModel GetUser(this ClaimsPrincipal user)
            {
                return new UserClaimsViewModel (
                    //user.GetUserId(),
                    //user.GetUserEmail(),
                    //user.GetUserRole()
                    1,
                    "admin@hotel.com",
                    "admin"
                );
            }
        }
}

