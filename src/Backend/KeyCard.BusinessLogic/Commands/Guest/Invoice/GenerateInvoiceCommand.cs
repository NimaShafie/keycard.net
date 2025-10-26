using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.Commands.Guest.Rooms;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;
using KeyCard.BusinessLogic.ViewModels.RequestClaims;
using KeyCard.BusinessLogic.ViewModels.Rooms;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Guest.Invoice
{
    public record GenerateInvoiceCommand (int BookingId) : Request, IRequest<InvoiceViewModel>;

    public class GenerateInvoiceCommandHandler : IRequestHandler<GenerateInvoiceCommand, InvoiceViewModel>
    {
        public IInvoiceService _invoiceService;
        public GenerateInvoiceCommandHandler(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        public async Task<InvoiceViewModel> Handle(GenerateInvoiceCommand command, CancellationToken cancellationToken)
        {
            return await _invoiceService.GenerateInvoiceAsync(command, cancellationToken);
        }
    }
}
