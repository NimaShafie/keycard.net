using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Core.Common;

namespace KeyCard.BusinessLogic.ViewModels.Booking
{
    public record BookingViewModel(
        int Id,
        string ConfirmationCode,
        DateTime CheckInDate,
        DateTime CheckOutDate,
        BookingStatus Status,
        string GuestName,
        string RoomNumber,
        decimal TotalAmount
    );
}
