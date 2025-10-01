// Models/HousekeepingTask.cs
namespace KeyCard.Desktop.Models
{
    public sealed class HousekeepingTask
    {
        // Use simple fields that are easy to bind in the UI.
        public string Id { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public string Title { get; set; } = string.Empty;

        // If your interface expects a string, change this to string; but most likely it's the enum:
        public HkTaskStatus Status { get; set; } = HkTaskStatus.Pending;

        // Optional niceties you can keep or remove as needed:
        public string? Notes { get; set; }
        public string? AssignedTo { get; set; }
    }
}
