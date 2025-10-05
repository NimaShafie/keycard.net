using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Identity;
using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models.Users
{
    public class GuestProfile : IDeletable, IAuditable
    {
        public Guid Id { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = default!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

    }

}
