using KeyCard.BusinessLogic.ViewModels.UserClaims;

namespace KeyCard.BusinessLogic.ViewModels.RequestClaims
{
    public record Request
    {
        public Request() { }
        /// <summary>
        /// Gets or sets <see cref="UserClaimsViewModel"/> model.
        /// </summary>
        public UserClaimsViewModel? User { get; set; } = null;
    }
}
