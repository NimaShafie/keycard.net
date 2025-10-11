using Microsoft.AspNetCore.Identity;

namespace KeyCard.Infrastructure.Models.User
{
    public class ApplicationUserRole : IdentityRole<int>
    {
        public ApplicationUserRole() : base() { }
        public ApplicationUserRole(string roleName) : base(roleName) { }
    }
}
