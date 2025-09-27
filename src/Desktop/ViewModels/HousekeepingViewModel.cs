// ViewModels/HousekeepingViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels;

public sealed class HousekeepingViewModel : ViewModelBase
{
    private readonly IHousekeepingService _svc;
    public ObservableCollection<Room> Rooms { get; } = new();
    public ObservableCollection<HousekeepingTask> Tasks { get; } = new();
    public Room? SelectedRoom { get => _room; set => Set(ref _room, value); }
    private Room? _room;
    public HousekeepingTask? SelectedTask { get => _task; set => Set(ref _task, value); }
    private HousekeepingTask? _task;

    public ICommand RefreshCommand { get; }
    public ICommand MarkRoomCleanCommand { get; }
    public ICommand MarkTaskDoneCommand { get; }

    public HousekeepingViewModel(IHousekeepingService svc)
    {
        _svc = svc;
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        MarkRoomCleanCommand = new RelayCommand(async _ => await RoomCleanAsync(), _ => SelectedRoom is not null);
        MarkTaskDoneCommand = new RelayCommand(async _ => await TaskDoneAsync(), _ => SelectedTask is not null);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Rooms.Clear(); foreach (var r in await _svc.GetRoomsAsync()) Rooms.Add(r);
        Tasks.Clear(); foreach (var t in await _svc.GetTasksAsync()) Tasks.Add(t);
    }
    private async Task RoomCleanAsync()
    {
        if (SelectedRoom is null) return;
        if (await _svc.UpdateRoomStatusAsync(SelectedRoom.Number, RoomStatus.Available))
            SelectedRoom = SelectedRoom with { Status = RoomStatus.Available };
    }
    private async Task TaskDoneAsync()
    {
        if (SelectedTask is null) return;
        if (await _svc.UpdateTaskStatusAsync(SelectedTask.TaskId, HkTaskStatus.Done))
            SelectedTask = SelectedTask with { Status = HkTaskStatus.Done };
    }
}
