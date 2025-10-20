// ViewModels/HousekeepingViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        public HousekeepingKanbanAdapter Kanban { get; }

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

            // Initialize adapter and connect base actions
            Kanban = new HousekeepingKanbanAdapter(this);

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
                        Description = string.IsNullOrWhiteSpace(task.Title)
                            ? $"Room {task.RoomId}"
                            : $"Room {task.RoomId} • {task.Title}",
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
                    Kanban.UpdateFrom(Tasks);
                }

                // Short-lived "Synced" blip on successful refresh
                SyncMessage = "Synced";
                _ = DispatcherTimer.RunOnce(() => SyncMessage = null, TimeSpan.FromSeconds(1.25));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                LoadMockData();
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
                    Kanban.UpdateFrom(Tasks); // reflect in Kanban
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
            Kanban.UpdateFrom(Tasks);
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

    // Row view models (unchanged)
    public partial class RoomRow : ViewModelBase
    {
        private int _number;
        public int Number { get => _number; set => SetProperty(ref _number, value); }

        private string _status = "Unknown";
        public string Status { get => _status; set => SetProperty(ref _status, value); }

        private bool? _isClean;
        public bool? IsClean { get => _isClean; set => SetProperty(ref _isClean, value); }
    }

    public partial class TaskRow : ViewModelBase
    {
        private string _id = string.Empty;
        public string Id { get => _id; set => SetProperty(ref _id, value); }

        private int _roomNumber;
        public int RoomNumber { get => _roomNumber; set => SetProperty(ref _roomNumber, value); }

        private string _description = string.Empty;
        public string Description { get => _description; set => SetProperty(ref _description, value); }

        private HkTaskStatus _status = HkTaskStatus.Pending;
        public HkTaskStatus Status { get => _status; set => SetProperty(ref _status, value); }

        private bool? _isCompleted;
        public bool? IsCompleted { get => _isCompleted; set => SetProperty(ref _isCompleted, value); }
    }

    // ===== Kanban adapter (UPDATED) =====
    // Exposes the members that HousekeepingKanbanView expects.
    public sealed class HousekeepingKanbanAdapter : ViewModelBase
    {
        private readonly HousekeepingViewModel _parent;

        public ObservableCollection<HousekeepingTask> Pending { get; } = new();
        public ObservableCollection<HousekeepingTask> InProgress { get; } = new();
        public ObservableCollection<HousekeepingTask> Completed { get; } = new();

        // Filter/search text
        private string? _filterText;
        public string? FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    // Refresh the projected views
                    OnPropertyChanged(nameof(PendingView));
                    OnPropertyChanged(nameof(InProgressView));
                    OnPropertyChanged(nameof(CompletedView));
                }
            }
        }

        // Projected (filtered) views used by ListBoxes
        public System.Collections.Generic.IEnumerable<HousekeepingTask> PendingView => ApplyFilter(Pending);
        public System.Collections.Generic.IEnumerable<HousekeepingTask> InProgressView => ApplyFilter(InProgress);
        public System.Collections.Generic.IEnumerable<HousekeepingTask> CompletedView => ApplyFilter(Completed);

        private System.Collections.Generic.IEnumerable<HousekeepingTask> ApplyFilter(System.Collections.Generic.IEnumerable<HousekeepingTask> src)
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return src;
            var q = FilterText.Trim();
            return src.Where(t =>
                (t.RoomId.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(t.Title) && t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(t.Notes) && t.Notes!.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(t.AssignedTo) && t.AssignedTo!.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        // Mock attendants (shown in dropdowns)
        public ObservableCollection<string> StaffOptions { get; } = new()
        {
            "— Unassigned —",
            "A. Patel",
            "B. Nguyen",
            "C. Lopez",
            "D. Kim",
            "E. Johnson",
            "F. Singh"
        };

        // Optional banners/fields so bindings don't error
        public bool IsMock { get; set; } = true;
        public string? NewRoom { get; set; }
        public string? NewTitle { get; set; }
        public string? NewNotes { get; set; }

        // Commands expected by the Kanban XAML
        public ICommand RefreshCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand AssignAttendantCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand DropOnPendingCommand { get; }
        public ICommand DropOnInProgressCommand { get; }
        public ICommand DropOnCompletedCommand { get; }

        public HousekeepingKanbanAdapter(HousekeepingViewModel parent)
        {
            _parent = parent;

            RefreshCommand = parent.RefreshCommand;

            AddTaskCommand = new UnifiedRelayCommand(() =>
            {
                // Basic "add" into Pending only (non-persistent placeholder)
                var rid = 0;
                int.TryParse(NewRoom, out rid);
                var item = new HousekeepingTask
                {
                    Id = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                    RoomId = rid,
                    Title = NewTitle ?? string.Empty,
                    Notes = NewNotes,
                    Status = HkTaskStatus.Pending,
                    AssignedTo = "— Unassigned —"
                };
                Pending.Add(item);

                // Reset inputs
                NewRoom = NewTitle = NewNotes = null;
                OnPropertyChanged(nameof(NewRoom));
                OnPropertyChanged(nameof(NewTitle));
                OnPropertyChanged(nameof(NewNotes));
                // Keep views fresh
                OnPropertyChanged(nameof(PendingView));
            });

            AssignAttendantCommand = new UnifiedRelayCommand(o =>
            {
                if (o is HousekeepingTask)
                {
                    // No persistence yet; VM already two-way binds AssignedTo.
                    // Raise change for filter projections (if filtering by attendant).
                    OnPropertyChanged(nameof(PendingView));
                    OnPropertyChanged(nameof(InProgressView));
                    OnPropertyChanged(nameof(CompletedView));
                }
            });

            DeleteTaskCommand = new UnifiedRelayCommand(o =>
            {
                if (o is string id)
                {
                    RemoveFromAll(id);
                }
                else if (o is HousekeepingTask t)
                {
                    RemoveFromAll(t.Id);
                }
                // refresh views
                OnPropertyChanged(nameof(PendingView));
                OnPropertyChanged(nameof(InProgressView));
                OnPropertyChanged(nameof(CompletedView));
            });

            DropOnPendingCommand = new UnifiedRelayCommand(o => MoveTo(o, HkTaskStatus.Pending));
            DropOnInProgressCommand = new UnifiedRelayCommand(o => MoveTo(o, HkTaskStatus.InProgress));
            DropOnCompletedCommand = new UnifiedRelayCommand(o => MoveTo(o, HkTaskStatus.Completed));
        }

        public void UpdateFrom(ObservableCollection<TaskRow> tasks)
        {
            Pending.Clear(); InProgress.Clear(); Completed.Clear();

            foreach (var t in tasks)
            {
                var mapped = new HousekeepingTask
                {
                    Id = t.Id,
                    RoomId = t.RoomNumber,
                    Title = t.Description,
                    Notes = null,
                    Status = t.Status,
                    AssignedTo = "— Unassigned —"
                };

                switch (t.Status)
                {
                    case HkTaskStatus.Completed: Completed.Add(mapped); break;
                    case HkTaskStatus.InProgress: InProgress.Add(mapped); break;
                    default: Pending.Add(mapped); break;
                }
            }

            OnPropertyChanged(nameof(PendingView));
            OnPropertyChanged(nameof(InProgressView));
            OnPropertyChanged(nameof(CompletedView));
        }

        public HousekeepingTask? FindById(string id)
        {
            return Pending.FirstOrDefault(x => x.Id == id)
                ?? InProgress.FirstOrDefault(x => x.Id == id)
                ?? Completed.FirstOrDefault(x => x.Id == id);
        }

        private void MoveTo(object? o, HkTaskStatus target)
        {
            if (o is not HousekeepingTask t) return;

            RemoveFromAll(t.Id);
            t.Status = target;

            switch (target)
            {
                case HkTaskStatus.Completed: Completed.Add(t); break;
                case HkTaskStatus.InProgress: InProgress.Add(t); break;
                default: Pending.Add(t); break;
            }

            // Reflect back into parent.Tasks for two-way sync:
            var row = _parent.Tasks.FirstOrDefault(x => x.Id == t.Id);
            if (row is not null)
            {
                row.Status = target;
                row.IsCompleted = target == HkTaskStatus.Completed;
            }

            OnPropertyChanged(nameof(PendingView));
            OnPropertyChanged(nameof(InProgressView));
            OnPropertyChanged(nameof(CompletedView));
        }

        private void RemoveFromAll(string id)
        {
            RemoveById(Pending, id);
            RemoveById(InProgress, id);
            RemoveById(Completed, id);
        }

        private static void RemoveById(ObservableCollection<HousekeepingTask> list, string id)
        {
            var item = list.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (item is not null) list.Remove(item);
        }
    }
}
