// ViewModels/HousekeepingViewModel.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    /// <summary>
    /// Matches Views/HousekeepingView.axaml bindings:
    /// - ObservableCollection<string> Rooms
    /// - string? SelectedRoom
    /// - ObservableCollection<string> Tasks
    /// - string? SelectedTask
    /// - Commands: MarkRoomCleanCommand, MarkTaskDoneCommand, RefreshCommand
    /// No compile-time dependency on specific IHousekeepingService methods
    /// (supports ListRoomsAsync, GetRoomsAsync, ListRooms, GetRooms, Rooms, etc.).
    /// </summary>
    public partial class HousekeepingViewModel : ViewModelBase
    {
        private readonly IHousekeepingService _svc;
        private readonly IAppEnvironment _env;

        // Preferred: static readonly arrays to avoid CA1861 (“constant array arguments”) warnings
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

            _ = RefreshAsync();
        }

        public ObservableCollection<string> Rooms { get; }
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

        public ObservableCollection<string> Tasks { get; }
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

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                Rooms.Clear();
                Tasks.Clear();

                var rooms = await TryGetListOfStringsAsync(_svc, AsyncRoomMethods)
                            ?? TryGetListOfStrings(_svc, RoomMethods)
                            ?? TryReadListProperty(_svc, RoomProps);

                var tasks = await TryGetListOfStringsAsync(_svc, AsyncTaskMethods)
                            ?? TryGetListOfStrings(_svc, TaskMethods)
                            ?? TryReadListProperty(_svc, TaskProps);

                if (rooms is { Count: > 0 })
                    foreach (var r in rooms) Rooms.Add(r);
                else
                    AddMockRooms();

                if (tasks is { Count: > 0 })
                    foreach (var t in tasks) Tasks.Add(t);
                else
                    AddMockTasks();
            }
            catch
            {
                // Safe fallback
                Rooms.Clear();
                Tasks.Clear();
                AddMockRooms();
                AddMockTasks();
            }
        }

        [RelayCommand]
        private void MarkRoomClean()
        {
            if (SelectedRoom is null) return;
            var label = SelectedRoom;
            if (!label.Contains("(Clean)", StringComparison.OrdinalIgnoreCase))
            {
                var idx = Rooms.IndexOf(label);
                if (idx >= 0) Rooms[idx] = $"{label} (Clean)";
            }
        }

        [RelayCommand]
        private void MarkTaskDone()
        {
            if (SelectedTask is null) return;
            var idx = Tasks.IndexOf(SelectedTask);
            if (idx >= 0) Tasks[idx] = $"{SelectedTask} (Done)";
        }

        // --- Reflection helpers to tolerate multiple service shapes ---

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
                catch
                {
                    // try next
                }
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
                catch
                {
                    // try next
                }
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
                catch
                {
                    // try next
                }
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

        // --- Mock helpers ---

        private void AddMockRooms()
        {
            Rooms.Add("1204");
            Rooms.Add("0711");
            Rooms.Add("1502");
            Rooms.Add("0808");
        }

        private void AddMockTasks()
        {
            Tasks.Add("Deliver extra towels to 0711");
            Tasks.Add("Deep clean 1502");
            Tasks.Add("Replace linens in 1204");
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
