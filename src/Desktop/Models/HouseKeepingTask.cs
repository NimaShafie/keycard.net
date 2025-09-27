// Models/HousekeepingTask.cs
using System;

namespace KeyCard.Desktop.Models
{
    public enum HkTaskStatus { Pending, InProgress, Done }

    public record HousekeepingTask(string TaskId, int RoomNumber, string Description, HkTaskStatus Status, DateTime CreatedAt);
}
