// ============================================================================
// USER CLAIMS EXTENSION - JWT TOKEN HELPER
// when user logs in, their info is stored in JWT token as "claims"
// these extension methods help extract that info easily
// ============================================================================

using System.Security.Claims;

using KeyCard.BusinessLogic.ViewModels.UserClaims;

namespace KeyCard.Api.Helper
{
    /// <summary>
    /// Extension methods to extract user information from JWT claims
    /// Makes controller code much cleaner - just call User.GetUserId() instead of
    /// digging through claims manually every time
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Get the user's ID from their JWT token
        /// This is unique identifier in our database
        /// </summary>
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // if no ID claim, something is very wrong with the token
            if (idClaim == null)
                throw new UnauthorizedAccessException("User ID claim not found.");

            return int.Parse(idClaim);
        }

        /// <summary>
        /// Get user's email from token
        /// Useful for sending notifications or looking up by email
        /// </summary>
        public static string GetUserEmail(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Get user's role - Admin, Guest, HouseKeeping, etc.
        /// Note: user can have multiple roles, this returns first one
        /// </summary>
        public static string GetUserRole(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Bundle all user info into a nice ViewModel
        /// Pass this to command handlers so they know who is making the request
        /// Great for audit logging - we always know WHO did WHAT
        /// </summary>
        public static UserClaimsViewModel GetUser(this ClaimsPrincipal user)
        {
            return new UserClaimsViewModel(
                user.GetUserId(),
                user.GetUserEmail(),
                user.GetUserRole()
            );
        }
    }
}

