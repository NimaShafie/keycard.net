using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;

using MediatR;

namespace KeyCard.BusinessLogic.Commands.Bookings
{
    public record GetBookingByIdCommand (int BookingId) : IRequest<BookingDto>;

    public class GetBookingByIdCommandHandler : IRequestHandler<GetBookingByIdCommand, BookingDto>
    {
        private readonly IBookingService _bookingService;
        public GetBookingByIdCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<BookingDto> Handle(GetBookingByIdCommand command, CancellationToken cancellationToken)
        {
            return await _bookingService.GetBookingByIdAsync(command, cancellationToken);
        }
    }
}
