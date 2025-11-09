// /ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Views;

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBookingService _bookings;
    private readonly ISignalRService _signalR;
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Booking> BookingItems { get; } = new();

    [ObservableProperty] private ViewModelBase? current;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private string displayName = string.Empty;
    [ObservableProperty] private string modeLabel = string.Empty;

    // Page tracking properties for button highlighting
    [ObservableProperty] private bool isOnDashboard;
    [ObservableProperty] private bool isOnFrontDesk;
    [ObservableProperty] private bool isOnHousekeeping;
    [ObservableProperty] private bool isOnFolio;

    // Global search text with real-time filtering
    private string? _globalSearchText;
    public string? GlobalSearchText
    {
        get => _globalSearchText;
        set
        {
            if (SetProperty(ref _globalSearchText, value))
            {
                // Trigger real-time search on child ViewModels
                TriggerChildSearch(value);
            }
        }
    }

    // Refresh indicator properties
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string? refreshStatusMessage;

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
        IServiceProvider serviceProvider)
    {
        _bookings = bookings;
        _signalR = signalR;
        _auth = auth;
        _nav = nav;
        _serviceProvider = serviceProvider;

        // Determine mode label
        var env = serviceProvider.GetService<IAppEnvironment>();
        ModeLabel = env?.IsMock == true ? "MOCK MODE" : "LIVE";

        // reflect initial state & subscribe to changes
        ApplyAuth();
        _auth.StateChanged += (_, __) => ApplyAuth();

        // Schedule initial navigation
        Dispatcher.UIThread.Post(() =>
        {
            if (!_auth.IsAuthenticated)
                _nav.NavigateTo<LoginViewModel>();
            else
                _nav.NavigateTo<DashboardViewModel>();
        }, DispatcherPriority.Background);
    }

    partial void OnCurrentChanged(ViewModelBase? value)
    {
        // Update page tracking when navigation occurs
        UpdatePageTracking();

        // Apply current search filter to new view
        if (!string.IsNullOrWhiteSpace(GlobalSearchText))
        {
            TriggerChildSearch(GlobalSearchText);
        }
    }

    private void UpdatePageTracking()
    {
        IsOnDashboard = Current is DashboardViewModel;
        IsOnFrontDesk = Current is FrontDeskViewModel;
        IsOnHousekeeping = Current is HousekeepingViewModel;
        IsOnFolio = Current?.GetType().Name == "FolioViewModel"; // Handle Folio module
    }

    private void ApplyAuth()
    {
        IsAuthenticated = _auth.IsAuthenticated;
        DisplayName = _auth.DisplayName ?? string.Empty;
        OnPropertyChanged(nameof(AvatarInitials));
    }

    private void TriggerChildSearch(string? searchText)
    {
        // Pass search filter to the current view model
        if (Current is DashboardViewModel dashboard)
        {
            dashboard.SearchText = searchText;
        }
        else if (Current is FrontDeskViewModel frontDesk)
        {
            frontDesk.SearchText = searchText;
        }
        else if (Current is HousekeepingViewModel housekeeping)
        {
            housekeeping.SearchText = searchText;
        }
        // Add other view models as needed
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

    [RelayCommand]
    private async Task RefreshCurrentView()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            RefreshStatusMessage = null;

            // Call refresh on the current view model using proper async invocation
            if (Current is DashboardViewModel dashboard)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (dashboard.RefreshCommand is Infrastructure.UnifiedRelayCommand cmd)
                    {
                        cmd.Execute(null);
                        await Task.Delay(500);
                    }
                });
            }
            else if (Current is FrontDeskViewModel frontDesk)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (frontDesk.RefreshCommand is Infrastructure.UnifiedRelayCommand cmd)
                    {
                        cmd.Execute(null);
                        await Task.Delay(500);
                    }
                });
            }
            else if (Current is HousekeepingViewModel housekeeping)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (housekeeping.RefreshCommand is Infrastructure.UnifiedRelayCommand cmd)
                    {
                        cmd.Execute(null);
                        await Task.Delay(500);
                    }
                });
            }

            // Show success message (NO EMOJI)
            RefreshStatusMessage = "Synced";

            // Clear success message after 2 seconds
            await Task.Delay(2000);
            RefreshStatusMessage = null;
        }
        catch (Exception ex)
        {
            RefreshStatusMessage = "Failed";
            System.Diagnostics.Debug.WriteLine($"Refresh failed: {ex.Message}");

            // Clear error message after 3 seconds
            await Task.Delay(3000);
            RefreshStatusMessage = null;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void OpenProfileMenu()
    {
        _ = IsAuthenticated;
        // Flyout opens automatically from XAML
    }

    [RelayCommand]
    private void OpenProfile() => _nav.NavigateTo<ProfileViewModel>();

    // Open Settings as an overlay window (modal), instead of navigating away.
    [RelayCommand]
    private async Task OpenSettings()
    {
        // Resolve the environment for SettingsViewModel
        var env = _serviceProvider.GetRequiredService<IAppEnvironment>();

        // Create the modal window, pass VM as DataContext
        var settingsWindow = new SettingsWindow(new SettingsViewModel(env));

        // Find owner (main window) for proper modal behavior
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (owner is not null)
            await settingsWindow.ShowDialog(owner);
        else
            settingsWindow.Show(); // Fallback (shouldn't happen in desktop lifetime)
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }

    [RelayCommand]
    private void NavigateDashboard() => _nav.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private void NavigateFrontDesk() => _nav.NavigateTo<FrontDeskViewModel>();

    [RelayCommand]
    private void NavigateHousekeeping() => _nav.NavigateTo<HousekeepingViewModel>();

    [RelayCommand]
    private void NavigateFolio() => NavigateFolioImpl();

    // Partial method to be implemented in MainViewModel.Folio.cs
    partial void NavigateFolioImpl();
}
