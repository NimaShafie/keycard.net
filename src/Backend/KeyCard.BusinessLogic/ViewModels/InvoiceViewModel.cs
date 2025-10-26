using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.BusinessLogic.ViewModels
{
    public record InvoiceViewModel (
        string InvoiceNumber,
        DateTime IssuedAt,
        string PdfPath,
        decimal TotalAmount,
        int BookingId
    );
}
