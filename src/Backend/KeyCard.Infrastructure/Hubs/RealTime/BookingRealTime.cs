using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Booking;
using Microsoft.AspNetCore.SignalR;


namespace KeyCard.Infrastructure.Hubs.RealTime
{
    public sealed class BookingsRealtime : IBookingsRealtime
    {
        private readonly IHubContext<BookingsHub, IBookingsClient> _hub;
        public BookingsRealtime(IHubContext<BookingsHub, IBookingsClient> hub) => _hub = hub;

        public async Task BookingUpdated(BookingViewModel dto, int? hotelId = null, int? bookingId = null)
        {
            var sends = new List<Task> { _hub.Clients.Group("role:FrontDesk").BookingUpdated(dto) };

            if (hotelId != null)
                sends.Add(_hub.Clients.Group($"hotel:{hotelId}").BookingUpdated(dto));

            if (bookingId != null)
                sends.Add(_hub.Clients.Group($"booking:{bookingId}").BookingUpdated(dto));

            await Task.WhenAll(sends);
        }

        public Task DigitalKeyCreated(DigitalKeyViewModel dto, int? hotelId = null, int? bookingId = null)
            => _hub.Clients.Group($"booking:{bookingId ?? dto.BookingId}").DigitalKeyCreated(dto);
    }
}
