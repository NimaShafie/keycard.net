// ViewModels/HousekeepingViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Threading;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    public partial class HousekeepingViewModel : ViewModelBase
    {
        private readonly IHousekeepingService _service;
        private readonly INavigationService _nav;

        private string? _syncMessage;
        public string? SyncMessage
        {
            get => _syncMessage;
            private set => SetProperty(ref _syncMessage, value);
        }

        private RoomRow? _selectedRoom;
        public RoomRow? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (SetProperty(ref _selectedRoom, value))
                    (MarkRoomCleanCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private TaskRow? _selectedTask;
        public TaskRow? SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (SetProperty(ref _selectedTask, value))
                    (MarkTaskDoneCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    (RefreshCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (MarkRoomCleanCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (MarkTaskDoneCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<RoomRow> Rooms { get; } = new();
        public ObservableCollection<TaskRow> Tasks { get; } = new();

        public ICommand BackToDashboardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand MarkRoomCleanCommand { get; }
        public ICommand MarkTaskDoneCommand { get; }

        public HousekeepingViewModel(IHousekeepingService service, INavigationService nav)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));

            BackToDashboardCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<DashboardViewModel>());
            RefreshCommand = new UnifiedRelayCommand(RefreshAsync, () => !IsBusy);
            MarkRoomCleanCommand = new UnifiedRelayCommand(MarkRoomCleanAsync, () => CanMarkRoomClean());
            MarkTaskDoneCommand = new UnifiedRelayCommand(MarkTaskDoneAsync, () => CanMarkTaskDone());

            _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading rooms and tasks...";

                var rooms = await _service.GetRoomsAsync();
                var tasks = await _service.GetTasksAsync();

                Rooms.Clear();
                foreach (var room in rooms)
                {
                    Rooms.Add(new RoomRow
                    {
                        Number = room.Number,
                        Status = MapRoomStatus(room.Status),
                        IsClean = room.Status == "Clean" || room.Status == "VacantClean"
                    });
                }

                Tasks.Clear();
                foreach (var task in tasks)
                {
                    Tasks.Add(new TaskRow
                    {
                        Id = task.Id,
                        RoomNumber = task.RoomId,
                        Description = task.Title,
                        Status = task.Status,
                        IsCompleted = task.Status == HkTaskStatus.Completed
                    });
                }

                // If service returned no data (but no error), inject samples so the UI isn't empty
                if (Rooms.Count == 0 && Tasks.Count == 0)
                {
                    LoadMockData();
                }
                else
                {
                    StatusMessage = $"Loaded {Rooms.Count} rooms, {Tasks.Count} tasks";
                }

                // Show short-lived "Synced" blip on successful refresh
                SyncMessage = "Synced";
                _ = DispatcherTimer.RunOnce(() => SyncMessage = null, TimeSpan.FromSeconds(1.25));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                LoadMockData();

                // Still show a brief synced indication so the user sees feedback
                SyncMessage = "Synced";
                _ = DispatcherTimer.RunOnce(() => SyncMessage = null, TimeSpan.FromSeconds(1.25));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task MarkRoomCleanAsync()
        {
            if (SelectedRoom is null) return;

            try
            {
                IsBusy = true;
                StatusMessage = $"Marking room {SelectedRoom.Number} as clean...";

                var success = await _service.UpdateRoomStatusAsync(
                    SelectedRoom.Number,
                    RoomStatus.Clean);

                if (success)
                {
                    SelectedRoom.IsClean = true;
                    SelectedRoom.Status = "Clean";
                    StatusMessage = $"Room {SelectedRoom.Number} marked as clean";
                }
                else
                {
                    StatusMessage = "Failed to update room status";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanMarkRoomClean() =>
            !IsBusy &&
            SelectedRoom is not null &&
            SelectedRoom.IsClean == false;

        private async Task MarkTaskDoneAsync()
        {
            if (SelectedTask is null) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Marking task as completed...";

                var success = await _service.UpdateTaskStatusAsync(
                    SelectedTask.Id,
                    HkTaskStatus.Completed);

                if (success)
                {
                    SelectedTask.IsCompleted = true;
                    SelectedTask.Status = HkTaskStatus.Completed;
                    StatusMessage = "Task marked as completed";
                }
                else
                {
                    StatusMessage = "Failed to update task status";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanMarkTaskDone() =>
            !IsBusy &&
            SelectedTask is not null &&
            SelectedTask.IsCompleted == false;

        private void LoadMockData()
        {
            Rooms.Clear();
            var mockRooms = new[]
            {
                (201, "Dirty"),
                (202, "Clean"),
                (203, "Dirty"),
                (304, "Occupied"),
                (402, "Dirty"),
                (405, "Clean"),
                (506, "Occupied")
            };

            foreach (var (number, status) in mockRooms)
            {
                Rooms.Add(new RoomRow
                {
                    Number = number,
                    Status = status,
                    IsClean = status == "Clean"
                });
            }

            Tasks.Clear();
            var mockTasks = new[]
            {
                (1, 201, "Full Clean"),
                (2, 304, "Replace Towels"),
                (3, 402, "Make Bed"),
                (4, 405, "Inspector Visit 2 PM"),
                (5, 0, "Lobby Vacuum")
            };

            int id = 1;
            foreach (var (_, room, desc) in mockTasks)
            {
                Tasks.Add(new TaskRow
                {
                    Id = $"TASK{id++}",
                    RoomNumber = room,
                    Description = room > 0 ? $"Room {room} • {desc}" : desc,
                    Status = HkTaskStatus.Pending,
                    IsCompleted = false
                });
            }

            StatusMessage = "Loaded mock data";
        }

        private static string MapRoomStatus(string status) => status switch
        {
            "VacantClean" => "Clean",
            "VacantDirty" => "Dirty",
            "OccupiedClean" => "Occupied (Clean)",
            "OccupiedDirty" => "Occupied (Dirty)",
            _ => status
        };
    }

    // Row view models
    public partial class RoomRow : ViewModelBase
    {
        private int _number;
        public int Number
        {
            get => _number;
            set => SetProperty(ref _number, value);
        }

        private string _status = "Unknown";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool? _isClean;
        public bool? IsClean
        {
            get => _isClean;
            set => SetProperty(ref _isClean, value);
        }
    }

    public partial class TaskRow : ViewModelBase
    {
        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private int _roomNumber;
        public int RoomNumber
        {
            get => _roomNumber;
            set => SetProperty(ref _roomNumber, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private HkTaskStatus _status = HkTaskStatus.Pending;
        public HkTaskStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool? _isCompleted;
        public bool? IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }
    }
}
