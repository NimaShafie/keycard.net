// /ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBookingService _bookings;
    private readonly ISignalRService _signalR;
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IServiceProvider _serviceProvider;  // ADDED for Folio

    public ObservableCollection<Booking> BookingItems { get; } = new();

    [ObservableProperty] private ViewModelBase? current;
    [ObservableProperty] private bool isBusy;

    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private string displayName = string.Empty;

    [ObservableProperty] private string modeLabel = string.Empty;  // ADDED for mode display

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
        IServiceProvider serviceProvider)  // ADDED parameter
    {
        _bookings = bookings;
        _signalR = signalR;
        _auth = auth;
        _nav = nav;
        _serviceProvider = serviceProvider;  // ADDED

        // ADDED: Determine mode label
        var env = serviceProvider.GetService<IAppEnvironment>();
        ModeLabel = env?.IsMock == true ? "MOCK MODE" : "LIVE";

        // reflect initial state & subscribe to changes
        ApplyAuth();
        _auth.StateChanged += (_, __) => ApplyAuth();

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

        // refresh computed initials
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
        // Touch instance state so CA1822 doesn't suggest 'static'
        _ = IsAuthenticated;
        // Flyout opening is handled by XAML; no further action needed here.
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

    // ADDED: Navigate to Folio (implementation in partial class)
    [RelayCommand]
    private void NavigateFolio() => NavigateFolioImpl();

    // Partial method to be implemented in MainViewModel.Folio.cs
    partial void NavigateFolioImpl();
}
