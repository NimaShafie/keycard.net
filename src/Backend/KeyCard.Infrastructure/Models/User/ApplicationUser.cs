using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Bookings;

using Microsoft.AspNetCore.Identity;

namespace KeyCard.Infrastructure.Models.User
{
    public class ApplicationUser : IdentityUser<int>, IAuditable, IDeletable
    {
        // Common fields for all users
        public string FirstName { get; set; } = default!;
        public string? LastName { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string? Address { get; set; }
        public string? Country { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Optional for staff
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public int? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
