using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Invoice
{
    public record GetInvoicePdfCommand (int BookingId) : IRequest<InvoiceViewModel>;

    public class GetInvoicePdfCommandHandler : IRequestHandler<GetInvoicePdfCommand, InvoiceViewModel>
    {
        public IInvoiceService _invoiceService;
        public GetInvoicePdfCommandHandler(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        public async Task<InvoiceViewModel> Handle(GetInvoicePdfCommand command, CancellationToken cancellationToken)
        {
            return await _invoiceService.GetInvoicePdfAsync(command, cancellationToken);
        }
    }
}
