// ViewModels/DashboardViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IBookingService _bookings;
    private readonly INavigationService _nav;

    public ObservableCollection<Booking> TodayArrivals { get; } = new();
    public ICommand GoFrontDesk { get; }
    public ICommand GoHousekeeping { get; }

    public DashboardViewModel(IBookingService bookings, INavigationService nav)
    {
        _bookings = bookings; _nav = nav;
        GoFrontDesk = new RelayCommand(_ => _nav.NavigateTo<FrontDeskViewModel>());
        GoHousekeeping = new RelayCommand(_ => _nav.NavigateTo<HousekeepingViewModel>());
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        TodayArrivals.Clear();
        foreach (var b in await _bookings.GetTodayArrivalsAsync()) TodayArrivals.Add(b);
    }
}
