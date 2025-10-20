// ViewModels/MainViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

    // --------------------------
    // 🔎 LIVE smoke-test helpers
    // --------------------------

    /// <summary>
    /// Simple LIVE health ping to /api/v1/Health. No-op in MOCK.
    /// Hook to a hidden dev button or call from Settings.
    /// </summary>
    [RelayCommand]
    public async Task TestHealthAsync(CancellationToken ct)
    {
        if (_env.IsMock || string.IsNullOrWhiteSpace(_env.ApiBaseUrl))
        {
            Debug.WriteLine("[TestHealth] Skipped (MOCK mode or empty ApiBaseUrl).");
            return;
        }

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(_env.ApiBaseUrl, UriKind.Absolute) };
            http.Timeout = TimeSpan.FromSeconds(5);
            var resp = await http.GetAsync("/api/v1/Health", ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            Debug.WriteLine($"[TestHealth] {(int)resp.StatusCode} {resp.ReasonPhrase} — {body}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[TestHealth] FAILED: " + ex.Message);
        }
    }

    /// <summary>
    /// Optional LIVE roundtrip:
    /// 1) If not authenticated, tries to login using KEYCARD_DEV_EMAIL/PASSWORD env vars.
    /// 2) Loads bookings (admin) via existing service.
    /// </summary>
    [RelayCommand]
    public async Task TestLiveRoundtripAsync(CancellationToken ct)
    {
        if (_env.IsMock)
        {
            Debug.WriteLine("[TestLiveRoundtrip] Skipped (MOCK mode).");
            return;
        }

        try
        {
            if (!_auth.IsAuthenticated)
            {
                var email = Environment.GetEnvironmentVariable("KEYCARD_DEV_EMAIL");
                var password = Environment.GetEnvironmentVariable("KEYCARD_DEV_PASSWORD");

                if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
                {
                    Debug.WriteLine($"[TestLiveRoundtrip] Attempting login for {email}…");
                    // Your IAuthService already exposes LoginAsync(string,string,ct)
                    await _auth.LoginAsync(email, password, ct);
                    ApplyAuth();
                    Debug.WriteLine($"[TestLiveRoundtrip] Login success? IsAuthenticated={_auth.IsAuthenticated}");
                }
                else
                {
                    Debug.WriteLine("[TestLiveRoundtrip] Missing KEYCARD_DEV_EMAIL/PASSWORD env vars; skipping login.");
                }
            }

            // Regardless of auth outcome, try to hit bookings to surface errors early
            var items = await _bookings.ListAsync(ct);

            // Avoid LINQ Count() on indexables (CA1829/CA1826)
            var count = FastCount(items);
            Debug.WriteLine($"[TestLiveRoundtrip] Bookings fetched: {count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[TestLiveRoundtrip] FAILED: " + ex);
        }
    }

    /// <summary>
    /// Uses Count property when available; otherwise falls back to LINQ Count().
    /// This avoids CA1829/CA1826 on indexable collections while remaining correct for any IEnumerable.
    /// </summary>
    private static int FastCount<T>(IEnumerable<T> source)
    {
        switch (source)
        {
            case ICollection<T> c: return c.Count;
            case IReadOnlyCollection<T> rc: return rc.Count;
            default: return source.Count(); // acceptable when not indexable
        }
    }
}
