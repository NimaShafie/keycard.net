using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models.Users
{
    public class GuestProfile : IDeletable
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public bool IsDeleted { get; set; }

    }

}
