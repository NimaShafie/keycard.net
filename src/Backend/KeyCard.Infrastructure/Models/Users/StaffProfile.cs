using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Identity;

namespace KeyCard.Infrastructure.Models.Users
{
    public class StaffProfile : IDeletable, IAuditable
    {
        public Guid Id { get; set; }
        public string Department { get; set; } = default!;  // e.g. FrontDesk, Housekeeping

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }

}
