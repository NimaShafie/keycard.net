// ViewModels/DashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Globalization;

using Avalonia.Collections;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IBookingService _bookings;
    private readonly INavigationService _nav;

    public ObservableCollection<Booking> TodayArrivals { get; } = new();

    // DataGrid-friendly view with sorting/filtering
    public DataGridCollectionView ArrivalsView { get; }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ArrivalsView.Filter = FilterBooking;
                ArrivalsView.Refresh();
            }
        }
    }

    public ICommand GoFrontDesk { get; }
    public ICommand GoHousekeeping { get; }
    public ICommand RefreshCommand { get; }

    public DashboardViewModel(IBookingService bookings, INavigationService nav)
    {
        _bookings = bookings; _nav = nav;

        ArrivalsView = new DataGridCollectionView(TodayArrivals)
        {
            Filter = FilterBooking
        };

        GoFrontDesk = new RelayCommand(_ => _nav.NavigateTo<FrontDeskViewModel>());
        GoHousekeeping = new RelayCommand(_ => _nav.NavigateTo<HousekeepingViewModel>());
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());

        _ = LoadAsync();
    }

    private bool FilterBooking(object? obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (obj is not Booking b) return false;

        var q = SearchText.Trim();

        // Convert non-nullables to strings directly
        var bookingIdStr = b.BookingId.ToString();
        var roomStr = b.RoomNumber.ToString(CultureInfo.InvariantCulture);

        return bookingIdStr.Contains(q, StringComparison.OrdinalIgnoreCase)
            || (b.GuestName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
            || roomStr.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadAsync()
    {
        TodayArrivals.Clear();
        var list = await _bookings.GetTodayArrivalsAsync();
        foreach (var b in list) TodayArrivals.Add(b);
        ArrivalsView.Refresh();
    }
}
