// ViewModels/MainViewModel.cs
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBookingService _bookings;
    private readonly ISignalRService _signalR;

    public ObservableCollection<Booking> BookingItems { get; } = new();

    [ObservableProperty] private ViewModelBase? current;

    [ObservableProperty] private bool isBusy;

    public MainViewModel(IBookingService bookings, ISignalRService signalR)
    {
        _bookings = bookings;
        _signalR = signalR;
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            var items = await _bookings.ListAsync(ct);
            BookingItems.Clear();
            foreach (var b in items) BookingItems.Add(b);

            await _signalR.StartAsync(ct);
            // Wire hub messages later
        }
        finally { IsBusy = false; }
    }
}
