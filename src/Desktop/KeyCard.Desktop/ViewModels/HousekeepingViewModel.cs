// ViewModels/HousekeepingViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;   // RelayCommand lives here
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for HousekeepingView.axaml
    /// Exposes: Rooms, SelectedRoom, Tasks, SelectedTask, and commands:
    ///   MarkRoomCleanCommand, MarkTaskDoneCommand, RefreshCommand
    /// </summary>
    public class HousekeepingViewModel : ViewModelBase
    {
        private readonly IHousekeepingService _service;

        public ObservableCollection<Room> Rooms { get; } = new();
        public ObservableCollection<HousekeepingTask> Tasks { get; } = new();

        private Room? _selectedRoom;
        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (_selectedRoom == value) return;
                _selectedRoom = value;
                OnPropertyChanged();
                (MarkRoomCleanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private HousekeepingTask? _selectedTask;
        public HousekeepingTask? SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (_selectedTask == value) return;
                _selectedTask = value;
                OnPropertyChanged();
                (MarkTaskDoneCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand MarkRoomCleanCommand { get; }
        public ICommand MarkTaskDoneCommand { get; }
        public ICommand RefreshCommand { get; }

        public HousekeepingViewModel(IHousekeepingService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            // Parameterless RelayCommand overloads (no command parameter in XAML)
            MarkRoomCleanCommand = new RelayCommand(
                execute: () => _ = MarkSelectedRoomCleanAsync(),
                canExecute: () => SelectedRoom is not null
            );

            MarkTaskDoneCommand = new RelayCommand(
                execute: () => _ = MarkSelectedTaskDoneAsync(),
                canExecute: () => SelectedTask is not null
            );

            RefreshCommand = new RelayCommand(
                execute: () => _ = RefreshAsync()
            );

            // Initial load
            _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            await LoadRoomsAsync().ConfigureAwait(false);
            await LoadTasksAsync().ConfigureAwait(false);
        }

        private async Task LoadRoomsAsync()
        {
            var rooms = await _service.GetRoomsAsync().ConfigureAwait(false);

            // Replace contents to preserve binding to the same ObservableCollection instance
            AppDispatch(() =>
            {
                Rooms.Clear();
                foreach (var r in rooms)
                    Rooms.Add(r);
            });
        }

        private async Task LoadTasksAsync()
        {
            var tasks = await _service.GetTasksAsync().ConfigureAwait(false);

            AppDispatch(() =>
            {
                Tasks.Clear();
                foreach (var t in tasks)
                    Tasks.Add(t);
            });
        }

        private async Task MarkSelectedRoomCleanAsync()
        {
            if (SelectedRoom is null) return;

            var roomId = TryGetRoomId(SelectedRoom);
            var ok = await _service.UpdateRoomStatusAsync(roomId, RoomStatus.Clean).ConfigureAwait(false);

            if (ok)
            {
                // Avoid mutating init-only properties on Room; reload from source
                await LoadRoomsAsync().ConfigureAwait(false);
            }
            // else: optionally surface an error toast/state later
        }

        private async Task MarkSelectedTaskDoneAsync()
        {
            if (SelectedTask is null) return;

            var ok = await _service.UpdateTaskStatusAsync(SelectedTask.Id, HkTaskStatus.Completed).ConfigureAwait(false);

            if (ok)
            {
                // If Status is init-only, refresh the list instead of mutating the item
                await LoadTasksAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attempts to extract an integer room id from common properties without
        /// requiring a specific Room model shape.
        /// Checks: Id, RoomId, Number, RoomNumber (string parse fallback).
        /// Returns 0 if none are found.
        /// </summary>
        private static int TryGetRoomId(Room room)
        {
            object? val = GetProp(room, "Id")
                          ?? GetProp(room, "RoomId")
                          ?? GetProp(room, "Number")
                          ?? GetProp(room, "RoomNumber");

            if (val is int i) return i;
            if (val is string s && int.TryParse(s, out var parsed)) return parsed;

            return 0;

            static object? GetProp(object obj, string name) =>
                obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(obj);
        }

        /// <summary>
        /// If you have a UI thread dispatcher, call it here. For Avalonia,
        /// you can replace this shim with Dispatcher.UIThread.Post(...).
        /// For now, we invoke directly to keep things simple.
        /// </summary>
        private static void AppDispatch(Action action) => action();

        // ViewModelBase already provides OnPropertyChanged / SetProperty
    }
}
