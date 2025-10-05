using KeyCard.Core.Common;
using KeyCard.Infrastructure.Identity;
using KeyCard.Infrastructure.Models.Entities;


namespace KeyCard.Infrastructure.Models.HouseKeeping
{
    public class HousekeepingTask : IDeletable, IAuditable
    {
        public Guid Id { get; set; }
        public string TaskName { get; set; } = default!;
        public string? Notes { get; set; }
        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Pending;
        public DateTime? CompletedAt { get; set; }

        public Guid RoomId { get; set; }
        public Room Room { get; set; } = default!;

        public Guid? AssignedToId { get; set; }
        public ApplicationUser? AssignedTo { get; set; } // Link to staff identity

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

    }

}
