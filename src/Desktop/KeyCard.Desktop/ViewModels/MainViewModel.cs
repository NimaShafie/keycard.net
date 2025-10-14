// ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

using Avalonia.Threading;

namespace KeyCard.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBookingService _bookings;
    private readonly ISignalRService _signalR;
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IAppEnvironment _env;

    public string ModeLabel { get; }
    public ObservableCollection<Booking> BookingItems { get; } = new();

    [ObservableProperty] private ViewModelBase? current;
    [ObservableProperty] private bool isBusy;

    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private string displayName = string.Empty;

    public string AvatarInitials => string.Join("",
        (DisplayName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(s => char.ToUpperInvariant(s[0])));

    public MainViewModel(
        IBookingService bookings,
        ISignalRService signalR,
        IAuthService auth,
        INavigationService nav,
        IAppEnvironment env)                 // <-- accept Services.IAppEnvironment
    {
        _bookings = bookings;
        _signalR = signalR;
        _auth = auth;
        _nav = nav;
        _env = env;                          // <-- set it

        // reflect initial state & subscribe to changes
        ApplyAuth();
        _auth.StateChanged += (_, __) => ApplyAuth();

        ModeLabel = _env.IsMock ? "MOCK" : "LIVE";

        // Schedule initial navigation so we don't block the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            if (!_auth.IsAuthenticated)
                _nav.NavigateTo<LoginViewModel>();
            else
                _nav.NavigateTo<DashboardViewModel>();
        }, DispatcherPriority.Background);
    }

    private void ApplyAuth()
    {
        IsAuthenticated = _auth.IsAuthenticated;
        DisplayName = _auth.DisplayName ?? string.Empty;
        OnPropertyChanged(nameof(AvatarInitials));
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
        }
        finally { IsBusy = false; }
    }

    // Profile menu
    [RelayCommand]
    private void OpenProfileMenu()
    {
        _ = IsAuthenticated; // keep instance reference
    }

    [RelayCommand]
    private void OpenProfile() => _nav.NavigateTo<ProfileViewModel>();

    [RelayCommand]
    private void OpenSettings() => _nav.NavigateTo<SettingsViewModel>();

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }

    // Top-left Dashboard button
    [RelayCommand]
    private void NavigateDashboard() => _nav.NavigateTo<DashboardViewModel>();
}
