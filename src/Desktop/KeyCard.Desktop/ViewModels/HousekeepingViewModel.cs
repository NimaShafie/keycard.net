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
        private readonly IToolbarService _toolbar;

        // ✅ SINGLETON STATE: Static instance to preserve state across navigation
        private static HousekeepingKanbanAdapter? _kanbanInstance;

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

        // SearchText property for MainViewModel compatibility
        public string? SearchText
        {
            get => Kanban.FilterText;
            set => Kanban.FilterText = value;
        }

        public ObservableCollection<RoomRow> Rooms { get; } = new();
        public ObservableCollection<TaskRow> Tasks { get; } = new();

        public ICommand BackToDashboardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand MarkRoomCleanCommand { get; }
        public ICommand MarkTaskDoneCommand { get; }

        public HousekeepingViewModel(IHousekeepingService service, INavigationService nav, IToolbarService toolbar)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _toolbar = toolbar;

            // ✅ Use singleton Kanban adapter to preserve state across navigation
            string initMessage;
            if (_kanbanInstance == null)
            {
                _kanbanInstance = new HousekeepingKanbanAdapter(this);
                initMessage = "🆕 NEW Kanban instance #1 created";
            }
            else
            {
                _kanbanInstance.SetParent(this);
                var assigned = _kanbanInstance.Pending.Concat(_kanbanInstance.InProgress).Concat(_kanbanInstance.Completed)
                    .Count(t => t.AssignedTo != "— Unassigned —");
                initMessage = $"♻️ REUSING Kanban | P={_kanbanInstance.Pending.Count}, IP={_kanbanInstance.InProgress.Count}, C={_kanbanInstance.Completed.Count} | {assigned} assigned";
            }

            StatusMessage = initMessage;
            Kanban = _kanbanInstance;

            BackToDashboardCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<DashboardViewModel>());
            RefreshCommand = new UnifiedRelayCommand(RefreshAsync, () => !IsBusy);
            MarkRoomCleanCommand = new UnifiedRelayCommand(MarkRoomCleanAsync, () => CanMarkRoomClean());
            MarkTaskDoneCommand = new UnifiedRelayCommand(MarkTaskDoneAsync, () => CanMarkTaskDone());

            _toolbar.AttachContext(
                title: "Housekeeping",
                subtitle: "Rooms & tasks",
                onRefreshAsync: RefreshAsync,
                onSearch: q => { Kanban.FilterText = q ?? string.Empty; },
                initialSearchText: Kanban?.FilterText
            );

            // ✅ Only load data on first visit
            var isFirstLoad = Rooms.Count == 0 && Tasks.Count == 0;
            if (isFirstLoad)
            {
                StatusMessage += " | Loading initial data...";
                _ = RefreshAsync();
            }
            else
            {
                StatusMessage += " | Data already loaded";
            }
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;

            // ✅ Save the initialization message
            var initMsg = StatusMessage;

            try
            {
                IsBusy = true;
                StatusMessage = initMsg + " | Loading rooms and tasks...";

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
                    var finalMsg = initMsg + $" | Loaded {Rooms.Count} rooms, {Tasks.Count} tasks";

                    // ✅ CRITICAL: Only rebuild Kanban if it's completely empty
                    var hasKanbanData = Kanban.Pending.Count > 0 || Kanban.InProgress.Count > 0 || Kanban.Completed.Count > 0;

                    if (!hasKanbanData)
                    {
                        finalMsg += " | Building Kanban for first time";
                        Kanban.UpdateFrom(Tasks);
                    }
                    else
                    {
                        finalMsg += $" | Kanban PRESERVED: P={Kanban.Pending.Count}, IP={Kanban.InProgress.Count}, C={Kanban.Completed.Count}";
                    }

                    StatusMessage = finalMsg;
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

                var roomNumber = SelectedRoom.Number;

                var success = await _service.UpdateRoomStatusAsync(
                    roomNumber,
                    RoomStatus.Clean);

                if (success)
                {
                    // ✅ Find the room in the collection and update it
                    var room = Rooms.FirstOrDefault(r => r.Number == roomNumber);
                    if (room != null)
                    {
                        room.IsClean = true;
                        room.Status = "Clean";
                    }

                    // ✅ Update Selected to point to the updated instance
                    SelectedRoom = room;

                    StatusMessage = $"Room {roomNumber} marked as clean";
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

                var taskId = SelectedTask.Id;

                var success = await _service.UpdateTaskStatusAsync(
                    taskId,
                    HkTaskStatus.Completed);

                if (success)
                {
                    // ✅ Find the task in the collection and update it
                    var task = Tasks.FirstOrDefault(t => t.Id == taskId);
                    if (task != null)
                    {
                        task.IsCompleted = true;
                        task.Status = HkTaskStatus.Completed;
                    }

                    // ✅ Update Selected to point to the updated instance
                    SelectedTask = task;

                    StatusMessage = "Task marked as completed";

                    // ✅ Update Kanban to reflect the change
                    var kanbanTask = Kanban.FindById(taskId);
                    if (kanbanTask != null)
                    {
                        Kanban.MoveTo(kanbanTask, HkTaskStatus.Completed);
                    }
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

            // ✅ Only build Kanban if empty
            if (Kanban.Pending.Count == 0 && Kanban.InProgress.Count == 0 && Kanban.Completed.Count == 0)
            {
                Kanban.UpdateFrom(Tasks);
            }
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

    // ===== Kanban adapter (FIXED WITH PROPER STATE PRESERVATION) =====
    public sealed class HousekeepingKanbanAdapter : ViewModelBase
    {
        private HousekeepingViewModel _parent;
        private static int _assignmentCounter;
        private static int _dragCounter;

        // ✅ BULLETPROOF: Static dictionary to store assignments
        private static readonly Dictionary<string, string> _assignments = new();

        // ✅ CRITICAL: Make collections STATIC so they survive ViewModel recreation
        private static ObservableCollection<HousekeepingTask>? _pendingStatic;
        private static ObservableCollection<HousekeepingTask>? _inProgressStatic;
        private static ObservableCollection<HousekeepingTask>? _completedStatic;

        public ObservableCollection<HousekeepingTask> Pending { get; }
        public ObservableCollection<HousekeepingTask> InProgress { get; }
        public ObservableCollection<HousekeepingTask> Completed { get; }

        private string? _filterText;
        public string? FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    OnPropertyChanged(nameof(PendingView));
                    OnPropertyChanged(nameof(InProgressView));
                    OnPropertyChanged(nameof(CompletedView));
                }
            }
        }

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

        public bool IsMock { get; set; } = true;
        public string? NewRoom { get; set; }
        public string? NewTitle { get; set; }
        public string? NewNotes { get; set; }

        public ICommand RefreshCommand { get; private set; }
        public ICommand AddTaskCommand { get; }
        public ICommand AssignAttendantCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand DropOnPendingCommand { get; }
        public ICommand DropOnInProgressCommand { get; }
        public ICommand DropOnCompletedCommand { get; }

        public HousekeepingKanbanAdapter(HousekeepingViewModel parent)
        {
            _parent = parent;

            // ✅ CRITICAL: Initialize from static fields or create new
            if (_pendingStatic == null)
            {
                _pendingStatic = new ObservableCollection<HousekeepingTask>();
                _inProgressStatic = new ObservableCollection<HousekeepingTask>();
                _completedStatic = new ObservableCollection<HousekeepingTask>();
                _parent.StatusMessage = "🆕 Kanban collections created (first time) | State will be PERMANENT";
            }
            else
            {
                _parent.StatusMessage = $"♻️ Kanban collections REUSED | P={_pendingStatic.Count}, IP={_inProgressStatic.Count}, C={_completedStatic.Count}";
            }

            // Point to the static collections
            Pending = _pendingStatic;
            InProgress = _inProgressStatic;
            Completed = _completedStatic;

            _parent.StatusMessage += $" | Total assignments so far: {_assignmentCounter}";

            RefreshCommand = parent.RefreshCommand;

            AddTaskCommand = new UnifiedRelayCommand(() =>
            {
                _ = int.TryParse(NewRoom, out var rid);
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
                _parent.StatusMessage = $"➕ Added new task: {item.Title} | Total pending: {Pending.Count}";

                NewRoom = NewTitle = NewNotes = null;
                OnPropertyChanged(nameof(NewRoom));
                OnPropertyChanged(nameof(NewTitle));
                OnPropertyChanged(nameof(NewNotes));
                OnPropertyChanged(nameof(PendingView));
            });

            AssignAttendantCommand = new UnifiedRelayCommand(o =>
            {
                if (o is HousekeepingTask task)
                {
                    _assignmentCounter++;

                    // ✅ Save to BOTH the object AND the static dictionary
                    if (!string.IsNullOrWhiteSpace(task.AssignedTo) && task.AssignedTo != "— Unassigned —")
                    {
                        _assignments[task.Id] = task.AssignedTo;
                        _parent.StatusMessage = $"👤 Assignment #{_assignmentCounter}: '{task.AssignedTo}' → {task.Title} | Saved to dictionary (total: {_assignments.Count})";
                    }
                    else
                    {
                        _parent.StatusMessage = $"⚠️ No staff selected for {task.Title}";
                        return;
                    }

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
                    _parent.StatusMessage = $"🗑️ Deleted task {id}";
                }
                else if (o is HousekeepingTask t)
                {
                    RemoveFromAll(t.Id);
                    _parent.StatusMessage = $"🗑️ Deleted task {t.Title}";
                }

                OnPropertyChanged(nameof(PendingView));
                OnPropertyChanged(nameof(InProgressView));
                OnPropertyChanged(nameof(CompletedView));
            });

            DropOnPendingCommand = new UnifiedRelayCommand(o =>
            {
                _dragCounter++;
                _parent.StatusMessage = $"🔄 Drag #{_dragCounter}: Moving to Pending...";
                MoveTo(o, HkTaskStatus.Pending);
            });

            DropOnInProgressCommand = new UnifiedRelayCommand(o =>
            {
                _dragCounter++;
                _parent.StatusMessage = $"🔄 Drag #{_dragCounter}: Moving to InProgress...";
                MoveTo(o, HkTaskStatus.InProgress);
            });

            DropOnCompletedCommand = new UnifiedRelayCommand(o =>
            {
                _dragCounter++;
                _parent.StatusMessage = $"🔄 Drag #{_dragCounter}: Moving to Completed...";
                MoveTo(o, HkTaskStatus.Completed);
            });
        }

        // ✅ Allow updating parent reference when ViewModel is recreated
        public void SetParent(HousekeepingViewModel parent)
        {
            _parent = parent;
            RefreshCommand = parent.RefreshCommand;

            // ✅ CRITICAL: Restore assignments from dictionary when reusing
            RestoreAssignments();
        }

        public void UpdateFrom(ObservableCollection<TaskRow> tasks)
        {
            var hasData = Pending.Count > 0 || InProgress.Count > 0 || Completed.Count > 0;

            if (hasData)
            {
                // ✅ NEVER rebuild if we already have data - this preserves ALL state
                var assignedCount = Pending.Count(t => t.AssignedTo != "— Unassigned —") +
                                   InProgress.Count(t => t.AssignedTo != "— Unassigned —") +
                                   Completed.Count(t => t.AssignedTo != "— Unassigned —");

                _parent.StatusMessage = $"✅ PRESERVED existing Kanban state (P={Pending.Count}, IP={InProgress.Count}, C={Completed.Count}) | {assignedCount} with assignments";

                // ✅ Debug: Show what assignments we have
                var assigned = Pending.Concat(InProgress).Concat(Completed)
                    .Where(t => t.AssignedTo != "— Unassigned —")
                    .Select(t => $"{t.Title}→{t.AssignedTo}")
                    .ToList();

                if (assigned.Any())
                {
                    _parent.StatusMessage += $" | Assignments: {string.Join(", ", assigned)}";
                }

                return;
            }

            // Only build on very first load
            _parent.StatusMessage = "🔨 First load - building Kanban from Tasks...";

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

            _parent.StatusMessage = $"✅ Kanban built: P={Pending.Count}, IP={InProgress.Count}, C={Completed.Count}";
        }

        public HousekeepingTask? FindById(string id)
        {
            return Pending.FirstOrDefault(x => x.Id == id)
                ?? InProgress.FirstOrDefault(x => x.Id == id)
                ?? Completed.FirstOrDefault(x => x.Id == id);
        }

        // ✅ Public method to save assignment directly
        public void SaveAssignment(string taskId, string staffName)
        {
            _assignments[taskId] = staffName;

            var task = FindById(taskId);
            if (task != null)
            {
                task.AssignedTo = staffName;
            }
        }

        // ✅ NEW: Restore assignments from dictionary
        public void RestoreAssignments()
        {
            _parent.StatusMessage = $"🔄 Restore called | Dict={_assignments.Count}";

            // ✅ Get all tasks FIRST
            var allTasks = Pending.Concat(InProgress).Concat(Completed).ToList();

            _parent.StatusMessage += $" Tasks={allTasks.Count}";

            // ✅ Restore assignments BEFORE clearing collections
            var restored = 0;
            foreach (var task in allTasks)
            {
                if (_assignments.TryGetValue(task.Id, out var assignedTo))
                {
                    task.AssignedTo = assignedTo;
                    restored++;
                }
            }

            _parent.StatusMessage = $"✅ Matched {restored} | Rebuilding...";

            if (restored > 0 || _assignments.Count > 0)
            {
                // ✅ NOW rebuild collections with assignments already set
                Pending.Clear();
                InProgress.Clear();
                Completed.Clear();

                // Re-add with assignments ALREADY on the objects
                foreach (var task in allTasks)
                {
                    switch (task.Status)
                    {
                        case HkTaskStatus.Pending:
                            Pending.Add(task);
                            break;
                        case HkTaskStatus.InProgress:
                            InProgress.Add(task);
                            break;
                        case HkTaskStatus.Completed:
                            Completed.Add(task);
                            break;
                    }
                }

                _parent.StatusMessage = $"✅ Restored {restored} | P={Pending.Count}, IP={InProgress.Count}, C={Completed.Count}";

                OnPropertyChanged(nameof(Pending));
                OnPropertyChanged(nameof(InProgress));
                OnPropertyChanged(nameof(Completed));
                OnPropertyChanged(nameof(PendingView));
                OnPropertyChanged(nameof(InProgressView));
                OnPropertyChanged(nameof(CompletedView));
            }
            else
            {
                _parent.StatusMessage = "ℹ️ Nothing to restore";
            }
        }

        public void MoveTo(object? o, HkTaskStatus target)
        {
            if (o is not HousekeepingTask t)
            {
                _parent.StatusMessage = $"❌ MoveTo failed: object is not HousekeepingTask (is {o?.GetType().Name ?? "null"})";
                return;
            }

            var oldStatus = t.Status;
            RemoveFromAll(t.Id);
            t.Status = target;

            switch (target)
            {
                case HkTaskStatus.Completed: Completed.Add(t); break;
                case HkTaskStatus.InProgress: InProgress.Add(t); break;
                default: Pending.Add(t); break;
            }

            var row = _parent.Tasks.FirstOrDefault(x => x.Id == t.Id);
            if (row is not null)
            {
                row.Status = target;
                row.IsCompleted = target == HkTaskStatus.Completed;
            }

            OnPropertyChanged(nameof(PendingView));
            OnPropertyChanged(nameof(InProgressView));
            OnPropertyChanged(nameof(CompletedView));

            _parent.StatusMessage = $"✅ Moved '{t.Title}' from {oldStatus} → {target} | P={Pending.Count}, IP={InProgress.Count}, C={Completed.Count}";
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
