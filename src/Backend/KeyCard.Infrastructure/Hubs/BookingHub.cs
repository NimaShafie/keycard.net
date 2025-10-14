using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.Infrastructure.Hubs.RealTime;

namespace KeyCard.Infrastructure.Hubs;

public interface IBookingsClient
{
    Task BookingUpdated(BookingViewModel dto);
    Task DigitalKeyCreated(DigitalKeyViewModel dto);
}
public class BookingsHub : KeyCardHubBase<IBookingsClient>
{
    public BookingsHub(ConnectionRegistry r) : base(r) { }
}
