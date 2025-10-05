using KeyCard.Infrastructure.Models.Users;

using Microsoft.AspNetCore.Identity;

namespace KeyCard.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        // Common fields for all users
        public string FullName { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public GuestProfile? GuestProfile { get; set; }  // if user is a guest
        public StaffProfile? StaffProfile { get; set; }  // if user is staff
    }
}
