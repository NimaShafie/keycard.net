using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Infrastructure.Models.Bookings;

namespace KeyCard.Infrastructure.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public DateTime IssuedAt { get; set; }
        public string PdfPath { get; set; } = default!;

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = default!;
    }

}
