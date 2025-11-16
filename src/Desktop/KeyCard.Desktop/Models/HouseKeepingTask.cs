// Models/HousekeepingTask.cs
using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.ViewModels;

namespace KeyCard.Desktop.Models
{
    /// <summary>
    /// Kanban task model with property change notifications
    /// </summary>
    public partial class HousekeepingTask : ViewModelBase
    {
        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private int _roomId;
        public int RoomId
        {
            get => _roomId;
            set => SetProperty(ref _roomId, value);
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string? _notes;
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private HkTaskStatus _status = HkTaskStatus.Pending;
        public HkTaskStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string? _assignedTo;
        public string? AssignedTo
        {
            get => _assignedTo;
            set => SetProperty(ref _assignedTo, value);
        }
    }
}
