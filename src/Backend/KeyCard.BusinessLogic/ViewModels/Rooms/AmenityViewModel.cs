using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.BusinessLogic.ViewModels.Rooms
{
    public record AmenityViewModel
    (
        string Key,
        string Label,
        string? Description,
        string? IconKey,
        string? Value
    );
}
