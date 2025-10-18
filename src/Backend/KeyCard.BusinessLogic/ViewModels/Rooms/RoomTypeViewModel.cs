using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.BusinessLogic.ViewModels.Rooms
{
    public record RoomTypeViewModel (
        int RoomTypeId,
        string Name,
        string Description,
        decimal PricePerNight,
        bool IsMostPopular,
        int MaxGuests,
        IReadOnlyList<AmenityViewModel> Amenities,
        decimal Total
    );
}
