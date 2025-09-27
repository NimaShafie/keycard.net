// ViewModels/FrontDeskViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels;

public sealed class FrontDeskViewModel : ViewModelBase
{
    private readonly IBookingService _svc;
    public ObservableCollection<Booking> Results { get; } = new();
    public string Query { get => _query; set => Set(ref _query, value); }
    private string _query = "";
    public int AssignRoomNumber { get => _room; set => Set(ref _room, value); }
    private int _room;
    public Booking? Selected { get => _sel; set => Set(ref _sel, value); }
    private Booking? _sel;

    public ICommand SearchCommand { get; }
    public ICommand AssignRoomCommand { get; }
    public ICommand CheckInCommand { get; }

    public FrontDeskViewModel(IBookingService svc)
    {
        _svc = svc;
        SearchCommand = new RelayCommand(async _ => await SearchAsync(), _ => !string.IsNullOrWhiteSpace(Query));
        AssignRoomCommand = new RelayCommand(async _ => await AssignAsync(), _ => Selected is not null && AssignRoomNumber > 0);
        CheckInCommand = new RelayCommand(async _ => await CheckInAsync(), _ => Selected is not null);
    }

    private async Task SearchAsync()
    {
        Results.Clear();
        var hit = await _svc.FindBookingByCodeAsync(Query);
        if (hit is not null) Results.Add(hit);
    }

    private async Task AssignAsync()
    {
        if (Selected is null) return;
        if (await _svc.AssignRoomAsync(Selected.BookingId, AssignRoomNumber))
            Selected = Selected with { RoomNumber = AssignRoomNumber };
    }

    private async Task CheckInAsync()
    {
        if (Selected is null) return;
        await _svc.CheckInAsync(Selected.BookingId);
    }
}
