using Microsoft.AspNetCore.Identity;

namespace KeyCard.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        // Common profile info
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Role/Department info
        public string? Department { get; set; }   // e.g., FrontDesk, Housekeeping
        public DateTime? HireDate { get; set; }  // for staff

        // Guest info
        public DateTime? DateOfBirth { get; set; }
        public string? PassportNumber { get; set; }   // optional for guests

        // Security / audit
        public bool IsActive { get; set; } = true;    // allow disabling accounts
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }
}
