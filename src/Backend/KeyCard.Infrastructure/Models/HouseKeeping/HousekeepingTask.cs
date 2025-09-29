using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.Users;
using KeyCard.Core.Common;


namespace KeyCard.Infrastructure.Models.HouseKeeping
{
    public class HousekeepingTask : IDeletable
    {
        public int Id { get; set; }
        public string Description { get; set; } = default!;
        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Pending;

        public int RoomId { get; set; }
        public Room Room { get; set; } = default!;

        public int? AssignedToId { get; set; }
        public StaffAccount? AssignedTo { get; set; }
        public bool IsDeleted { get; set; }

    }

}
