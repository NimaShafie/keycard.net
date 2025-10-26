using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.Commands.Guest.Invoice;
using KeyCard.BusinessLogic.ViewModels;

namespace KeyCard.BusinessLogic.ServiceInterfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceViewModel> GenerateInvoiceAsync(GenerateInvoiceCommand command, CancellationToken ct);
        Task<InvoiceViewModel> GetInvoicePdfAsync(GetInvoicePdfCommand command, CancellationToken ct);
    }
}
