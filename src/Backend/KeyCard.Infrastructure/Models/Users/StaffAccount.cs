using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;

namespace KeyCard.Infrastructure.Models.Users
{
    public class StaffAccount : IDeletable
    {
        public int Id { get; set; }
        public string Username { get; set; } = default!;
        public string Role { get; set; } = default!; // FrontDesk, Housekeeping, Admin
        public bool IsDeleted { get; set; }
    }

}
