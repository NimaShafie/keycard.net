using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.BusinessLogic.ViewModels.Task
{
    public record TaskViewModel(
        int Id,
        string TaskName,
        string? Notes,
        string Status,
        string RoomNumber,
        string? AssignedTo
    );
}
