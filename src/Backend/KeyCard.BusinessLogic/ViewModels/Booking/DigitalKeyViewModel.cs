using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.BusinessLogic.ViewModels.Booking
{
    public class DigitalKeyViewModel
    {
        public int Id { get; set; }
        public string Token { get; set; } = default!;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public int BookingId { get; set; }
    }
}
