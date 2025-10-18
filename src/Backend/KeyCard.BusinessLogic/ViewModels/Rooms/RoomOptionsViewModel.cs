using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.BusinessLogic.ViewModels.Rooms
{
    public record RoomOptionsViewModel (
        SearchContextViewModel Search,
        List<RoomTypeViewModel> Options
    );

    public record SearchContextViewModel (
        DateOnly CheckIn,
        DateOnly CheckOut,
        int Nights,
        int Guests,
        int Rooms,
        string Currency = "USD"
    );

}
