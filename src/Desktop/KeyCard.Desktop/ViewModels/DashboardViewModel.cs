// src/Desktop/KeyCard.Desktop/ViewModels/DashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Avalonia.Collections; // DataGridCollectionView
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    /// <summary>
    /// Matches your DashboardView.axaml:
    /// - Commands: GoFrontDesk, GoHousekeeping, RefreshCommand
    /// - Properties: SearchText, IsRefreshing
    /// - Items: ArrivalsView (DataGridCollectionView) bound to Booking
    /// - Seeds mock rows in Mock mode using reflection to respect readonly/typed fields
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private readonly IAppEnvironment _env;

        // Backing collection shown in the grid
        public ObservableCollection<Booking> Arrivals { get; } = new();

        // Avalonia DataGrid-friendly view
        public DataGridCollectionView ArrivalsView { get; }

        public DashboardViewModel(INavigationService nav, IAppEnvironment env)
        {
            _nav = nav;
            _env = env;

            ArrivalsView = new DataGridCollectionView(Arrivals);

            if (_env.IsMock)
            {
                TrySeedMockBookings();
            }
        }

        // ---- Properties (explicit setters; no source generators here) ----

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            private set
            {
                if (_isRefreshing == value) return;
                _isRefreshing = value;
                Raise(nameof(IsRefreshing));
            }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                Raise(nameof(SearchText));
                ApplyFilter();
            }
        }

        // ---- Commands ----

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            try
            {
                // Simulate I/O refresh
                await Task.Delay(800);

                if (_env.IsMock && Arrivals.Count == 0)
                    TrySeedMockBookings();

                ApplyFilter();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private void GoFrontDesk() => _nav.NavigateTo<FrontDeskViewModel>();

        [RelayCommand]
        private void GoHousekeeping() => _nav.NavigateTo<HousekeepingViewModel>();

        // ---- Helpers ----

        private void ApplyFilter()
        {
            var q = (SearchText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(q))
            {
                ArrivalsView.Filter = null;
                ArrivalsView.Refresh();
                return;
            }

            var needle = q.ToLowerInvariant();

            ArrivalsView.Filter = obj =>
            {
                if (obj is not Booking b) return false;

                // Try a few common fields (stringified) without assuming types
                var id = GetString(b, "BookingId") ?? GetString(b, "Id");
                var guest = GetString(b, "GuestName") ?? GetString(b, "Guest");
                var room = GetString(b, "RoomNumber") ?? GetString(b, "Room");

                return (id?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (guest?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (room?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);
            };

            ArrivalsView.Refresh();
        }

        private static string? GetString(object obj, string prop)
        {
            var pi = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (pi is null) return null;
            var val = pi.GetValue(obj);
            return val?.ToString();
        }

        private void TrySeedMockBookings()
        {
            // Try best-effort construction respecting readonly/typed properties.
            Arrivals.Clear();

            // Create 3 mock bookings with safe reflection
            Arrivals.Add(CreateBooking(
                id: "BK-100241",
                guest: "Alex Chen",
                room: "1204",
                checkIn: DateTime.Today,
                checkOut: DateTime.Today.AddDays(3)));

            Arrivals.Add(CreateBooking(
                id: "BK-100242",
                guest: "Priya N.",
                room: "0711",
                checkIn: DateTime.Today,
                checkOut: DateTime.Today.AddDays(2)));

            Arrivals.Add(CreateBooking(
                id: "BK-100243",
                guest: "J. Morales",
                room: "1502",
                checkIn: DateTime.Today,
                checkOut: DateTime.Today.AddDays(5)));
        }

        private static Booking CreateBooking(string id, string guest, string room, DateTime checkIn, DateTime checkOut)
        {
            // 1) Try parameterless ctor
            var b = Activator.CreateInstance<Booking>();
            if (b == null)
            {
                // 2) Try to find a constructor with common shapes
                foreach (var ctor in typeof(Booking).GetConstructors())
                {
                    try
                    {
                        var pars = ctor.GetParameters();
                        var args = new object?[pars.Length];
                        for (int i = 0; i < pars.Length; i++)
                        {
                            var p = pars[i];
                            args[i] = p.ParameterType switch
                            {
                                Type t when t == typeof(string) && p.Name!.Contains("booking", StringComparison.OrdinalIgnoreCase) => id,
                                Type t when t == typeof(string) && p.Name!.Contains("guest", StringComparison.OrdinalIgnoreCase) => guest,
                                Type t when (t == typeof(string) || t == typeof(int)) && p.Name!.Contains("room", StringComparison.OrdinalIgnoreCase)
                                    => (t == typeof(int) && int.TryParse(room, out var roomNumArg)) ? roomNumArg : room,
                                Type t when t.Name is "DateTime" && p.Name!.Contains("checkin", StringComparison.OrdinalIgnoreCase) => checkIn,
                                Type t when t.Name is "DateTime" && p.Name!.Contains("checkout", StringComparison.OrdinalIgnoreCase) => checkOut,
                                Type t when t.Name is "DateOnly" && p.Name!.Contains("checkin", StringComparison.OrdinalIgnoreCase)
                                    => CreateDateOnly(checkIn),
                                Type t when t.Name is "DateOnly" && p.Name!.Contains("checkout", StringComparison.OrdinalIgnoreCase)
                                    => CreateDateOnly(checkOut),
                                Type t when t == typeof(Guid) => Guid.NewGuid(),
                                _ => GetDefault(p.ParameterType)
                            };
                        }

                        var obj = ctor.Invoke(args);
                        if (obj is Booking created) return created;
                    }
                    catch
                    {
                        // try next ctor
                    }
                }

                // 3) As a last resort create an uninitialized object
#pragma warning disable SYSLIB0050
                b = (Booking)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Booking));
#pragma warning restore SYSLIB0050
            }

            // Try setting common properties if writable
            TrySet(b, "BookingId", id);
            TrySet(b, "Id", Guid.NewGuid());

            if (int.TryParse(room, out var roomNumParsed))
                TrySet(b, "RoomNumber", roomNumParsed);
            else
                TrySet(b, "RoomNumber", room);

            TrySet(b, "GuestName", guest);
            TrySet(b, "Guest", guest);

            TrySetDate(b, "CheckIn", checkIn);
            TrySetDate(b, "CheckOut", checkOut);

            return b;
        }

        private static void TrySet(object target, string prop, object? value)
        {
            var pi = target.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (pi is null || !pi.CanWrite) return;

            try
            {
                var destType = pi.PropertyType;
                object? converted = value;

                if (value != null && !destType.IsAssignableFrom(value.GetType()))
                {
                    if (destType == typeof(Guid) && value is string s)
                        converted = Guid.TryParse(s, out var g) ? g : Guid.NewGuid();
                    else if (destType == typeof(int) && value is string ss && int.TryParse(ss, out var iv))
                        converted = iv;
                    else
                        converted = Convert.ChangeType(value, destType, CultureInfo.InvariantCulture);
                }

                pi.SetValue(target, converted);
            }
            catch
            {
                // ignore if not settable/convertible
            }
        }

        private static void TrySetDate(object target, string prop, DateTime dt)
        {
            var pi = target.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (pi is null || !pi.CanWrite) return;

            try
            {
                if (pi.PropertyType.Name == "DateOnly")
                {
                    var dateOnly = CreateDateOnly(dt);
                    pi.SetValue(target, dateOnly);
                }
                else if (pi.PropertyType == typeof(DateTime))
                {
                    pi.SetValue(target, dt);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static object? CreateDateOnly(DateTime dt)
        {
            // Handle both net6+ DateOnly and absence gracefully
            var t = Type.GetType("System.DateOnly");
            if (t == null) return null;
            var ctor = t.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
            return ctor?.Invoke(new object[] { dt.Year, dt.Month, dt.Day });
        }

        private static object? GetDefault(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        // Try to call base's OnPropertyChanged if it exists
        private void Raise(string propertyName)
        {
            try
            {
                var mi = GetType().GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                mi ??= typeof(ViewModelBase).GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                mi?.Invoke(this, new object?[] { propertyName });
            }
            catch
            {
                // if base doesn't expose it, we silently skip
            }
        }
    }
}
