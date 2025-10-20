// Models/HousekeepingTaskDto.cs
using System;

namespace KeyCard.Desktop.Models
{
    public enum TaskStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2
    }

    public class HousekeepingTaskDto
    {
        public Guid Id { get; set; }
        public string? RoomNumber { get; set; }
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public string? Attendant { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? UpdatedUtc { get; set; }

        public override string ToString() => $"{RoomNumber}: {Title} ({Status})";
    }
}
