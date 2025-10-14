using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ViewModels.Booking;
using KeyCard.BusinessLogic.ViewModels.Task;

namespace KeyCard.BusinessLogic.ServiceInterfaces;

public interface IBookingsRealtime
{
    Task BookingUpdated(BookingViewModel dto, int? hotelId = null, int? bookingId = null);
    Task DigitalKeyCreated(DigitalKeyViewModel dto, int? hotelId = null, int? bookingId = null);
}
public interface IRoomsRealtime
{
    //Task RoomStatusChanged(RoomStatusChangedDto dto, string? hotelId = null, string? role = null);
}
public interface ITasksRealtime
{
    Task TaskLifecycleChanged(TaskViewModel dto, int? hotelId = null, string? role = null);
}
