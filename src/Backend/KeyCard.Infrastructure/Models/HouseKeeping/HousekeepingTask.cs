using KeyCard.Core.Common;
using KeyCard.Infrastructure.Identity;
using KeyCard.Infrastructure.Models.Entities;


namespace KeyCard.Infrastructure.Models.HouseKeeping
{
    public class HousekeepingTask : IDeletable, IAuditable
    {
        public int Id { get; set; }
        public string TaskName { get; set; } = default!;
        public string? Notes { get; set; }
        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Pending;
        public DateTime? CompletedAt { get; set; }

        public int? RoomId { get; set; }
        public Room? Room { get; set; } = default!;

        public Guid? AssignedToId { get; set; }
        public ApplicationUser? AssignedTo { get; set; } // Link to staff identity

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public Guid? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public void UpdateStatus(TaskStatusEnum status)
        {
            this.Status = status;
        }

    }

}
