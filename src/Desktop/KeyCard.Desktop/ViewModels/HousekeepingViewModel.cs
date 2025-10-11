// ViewModels/HousekeepingViewModel.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    public partial class HousekeepingViewModel : ViewModelBase
    {
        private readonly IHousekeepingService _svc;
        private readonly IAppEnvironment _env;


        public ObservableCollection<string> Rooms { get; }
        public ObservableCollection<string> Tasks { get; }
        public bool UseMockData { get; set; } = true;

        // String-based selections (kept for back-compat with older views)
        private string? _selectedRoom;
        public string? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (_selectedRoom == value) return;
                _selectedRoom = value;
                Raise(nameof(SelectedRoom));
            }
        }

        private string? _selectedTask;
        public string? SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (_selectedTask == value) return;
                _selectedTask = value;
                Raise(nameof(SelectedTask));
            }
        }

        // NEW: tabular sources for DataGrids (with Status column)
        public ObservableCollection<RoomRow> RoomsTable { get; }
        public ObservableCollection<TaskRow> TasksTable { get; }

        // NEW: row selections used by the DataGrids
        public RoomRow? SelectedRoomRow { get; set; }
        public TaskRow? SelectedTaskRow { get; set; }

        // Reflection candidates (unchanged)
        private static readonly string[] AsyncRoomMethods = { "ListRoomsAsync", "GetRoomsAsync" };
        private static readonly string[] RoomMethods = { "ListRooms", "GetRooms" };
        private static readonly string[] RoomProps = { "Rooms" };
        private static readonly string[] AsyncTaskMethods = { "ListTasksAsync", "GetTasksAsync" };
        private static readonly string[] TaskMethods = { "ListTasks", "GetTasks" };
        private static readonly string[] TaskProps = { "Tasks" };

        public HousekeepingViewModel(IHousekeepingService svc, IAppEnvironment env)
        {
            _svc = svc;
            _env = env;

            Rooms = new ObservableCollection<string>();
            Tasks = new ObservableCollection<string>();
            RoomsTable = new ObservableCollection<RoomRow>();
            TasksTable = new ObservableCollection<TaskRow>();

            _ = RefreshAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                Rooms.Clear();
                Tasks.Clear();
                RoomsTable.Clear();
                TasksTable.Clear();

                var rooms = await TryGetListOfStringsAsync(_svc, AsyncRoomMethods)
                            ?? TryGetListOfStrings(_svc, RoomMethods)
                            ?? TryReadListProperty(_svc, RoomProps);

                var tasks = await TryGetListOfStringsAsync(_svc, AsyncTaskMethods)
                            ?? TryGetListOfStrings(_svc, TaskMethods)
                            ?? TryReadListProperty(_svc, TaskProps);

                if (rooms is { Count: > 0 })
                {
                    foreach (var r in rooms)
                    {
                        Rooms.Add(r);
                        RoomsTable.Add(new RoomRow { Name = r, Status = null });
                    }
                }
                else
                {
                    AddMockRooms();
                }

                if (tasks is { Count: > 0 })
                {
                    foreach (var t in tasks)
                    {
                        Tasks.Add(t);
                        TasksTable.Add(new TaskRow { Description = t, Status = null });
                    }
                }
                else
                {
                    AddMockTasks();
                }
            }
            catch
            {
                Rooms.Clear();
                Tasks.Clear();
                RoomsTable.Clear();
                TasksTable.Clear();
                AddMockRooms();
                AddMockTasks();
            }
        }

        // SINGLE command that handles both the DataGrid row selection and the legacy string selection
        [RelayCommand]
        private void MarkRoomClean()
        {
            if (SelectedRoomRow is not null) { SelectedRoomRow.Status = true; return; }

            if (SelectedRoom is null) return;
            var row = RoomsTable.FirstOrDefault(r =>
                string.Equals(r.Name, SelectedRoom, StringComparison.OrdinalIgnoreCase));
            if (row is not null) row.Status = true;
        }

        // SINGLE command that handles both selection modes
        [RelayCommand]
        private void MarkTaskDone()
        {
            if (SelectedTaskRow is not null) { SelectedTaskRow.Status = true; return; }

            if (SelectedTask is null) return;
            var row = TasksTable.FirstOrDefault(t =>
                string.Equals(t.Description, SelectedTask, StringComparison.OrdinalIgnoreCase));
            if (row is not null) row.Status = true;
        }

        // --- Table row view models (Status is tri-state: null/unchecked/checked) ---
        public partial class RoomRow : ObservableObject
        {
            [ObservableProperty] private string name = "";
            [ObservableProperty] private bool? status;
        }

        public partial class TaskRow : ObservableObject
        {
            [ObservableProperty] private string description = "";
            [ObservableProperty] private bool? status;
        }

        // --- Reflection helpers (unchanged) ---
        private static async Task<List<string>?> TryGetListOfStringsAsync(object svc, string[] methodNames)
        {
            foreach (var name in methodNames)
            {
                var mi = svc.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null);
                if (mi is null) continue;

                try
                {
                    var result = mi.Invoke(svc, null);
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                        var resProp = task.GetType().GetProperty("Result");
                        var val = resProp?.GetValue(task);
                        return ToStringList(val);
                    }

                    return ToStringList(result);
                }
                catch { /* try next */ }
            }
            return null;
        }

        private static List<string>? TryGetListOfStrings(object svc, string[] methodNames)
        {
            foreach (var name in methodNames)
            {
                var mi = svc.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null);
                if (mi is null) continue;

                try
                {
                    var result = mi.Invoke(svc, null);
                    return ToStringList(result);
                }
                catch { /* try next */ }
            }
            return null;
        }

        private static List<string>? TryReadListProperty(object svc, string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var pi = svc.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (pi is null) continue;

                try
                {
                    var val = pi.GetValue(svc);
                    return ToStringList(val);
                }
                catch { /* try next */ }
            }
            return null;
        }

        private static List<string>? ToStringList(object? value)
        {
            if (value is null) return null;

            if (value is IEnumerable<string> strEnum)
                return strEnum.ToList();

            if (value is IEnumerable en)
            {
                var list = new List<string>();
                foreach (var item in en)
                {
                    if (item is null) continue;
                    list.Add(item.ToString() ?? string.Empty);
                }
                return list;
            }

            return null;
        }

        // --- Mock helpers (populate both string and table forms) ---
        private void AddMockRooms()
        {
            var rooms = new[] { "1204", "0711", "1502", "0808" };
            foreach (var r in rooms)
            {
                Rooms.Add(r);
                RoomsTable.Add(new RoomRow { Name = r, Status = null });
            }
        }

        private void AddMockTasks()
        {
            var tasks = new[]
            {
                "Deliver extra towels to 0711",
                "Deep clean 1502",
                "Replace linens in 1204"
            };
            foreach (var t in tasks)
            {
                Tasks.Add(t);
                TasksTable.Add(new TaskRow { Description = t, Status = null });
            }
        }

        private void Raise(string propertyName)
        {
            try
            {
                var mi = GetType().GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                         ?? typeof(ViewModelBase).GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                mi?.Invoke(this, new object?[] { propertyName });
            }
            catch { /* ignored */ }
        }
    }
}
