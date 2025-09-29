using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.Core.Common
{
    public enum RoomStatus
    {
        Vacant,
        Occupied,
        Dirty,
        Cleaning,
        Inspected,
        OutOfService
    }

    public enum BookingStatus
    {
        Reserved,
        CheckedIn,
        CheckedOut,
        Cancelled
    }

    public enum TaskStatusEnum
    {
        Pending,
        InProgress,
        Completed
    }
}
