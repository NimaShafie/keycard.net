// ViewModels/FrontDeskViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading; // UI thread marshaling

using CommunityToolkit.Mvvm.Input;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    /// <summary>
    /// Matches Views/FrontDeskView.axaml bindings:
    /// - string Query
    /// - ObservableCollection<Booking> Results
    /// - Booking? Selected
    /// - string AssignRoomNumber
    /// - Commands: SearchCommand, AssignRoomCommand, CheckInCommand
    /// </summary>
    public partial class FrontDeskViewModel : ViewModelBase
    {
        private readonly IBookingService _bookings;
        private readonly IAppEnvironment _env;

        public FrontDeskViewModel(IBookingService bookings, IAppEnvironment env)
        {
            _bookings = bookings;
            _env = env;

            Results = new ObservableCollection<Booking>();

            // Async, non-blocking initial data load
            _ = LoadInitialAsync();
        }

        // --- Properties ---

        private string? _query;
        public string? Query
        {
            get => _query;
            set
            {
                if (_query == value) return;
                _query = value;
                Raise(nameof(Query));
            }
        }

        public ObservableCollection<Booking> Results { get; }

        private Booking? _selected;
        public Booking? Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                Raise(nameof(Selected));
            }
        }

        private string? _assignRoomNumber;
        public string? AssignRoomNumber
        {
            get => _assignRoomNumber;
            set
            {
                if (_assignRoomNumber == value) return;
                _assignRoomNumber = value;
                Raise(nameof(AssignRoomNumber));
            }
        }

        // --- Commands ---

        [RelayCommand]
        private async Task SearchAsync()
        {
            try
            {
                var list = await _bookings.ListAsync(CancellationToken.None);
                var q = (Query ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(q))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Results.Clear();
                        foreach (var b in list) Results.Add(b);
                    });
                    return;
                }

                var filtered = list.Where(b =>
                {
                    var id = GetString(b, "BookingId") ?? GetString(b, "Id");
                    var guest = GetString(b, "GuestName") ?? GetString(b, "Guest");
                    var room = GetString(b, "RoomNumber") ?? GetString(b, "Room");
                    return Contains(id, q) || Contains(guest, q) || Contains(room, q);
                }).ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Results.Clear();
                    foreach (var b in filtered) Results.Add(b);
                });
            }
            catch
            {
                // Swallow transient errors to avoid UI crash
            }
        }

        [RelayCommand]
        private void AssignRoom()
        {
            if (Selected is null) return;

            var room = (AssignRoomNumber ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(room)) return;

            // Try int first; fall back to string if RoomNumber is not an int
            if (int.TryParse(room, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rn))
                TrySet(Selected, "RoomNumber", rn);
            else
                TrySet(Selected, "RoomNumber", room);
        }

        [RelayCommand]
        private void CheckIn()
        {
            if (Selected is null) return;

            var today = DateTime.Today;
            TrySetDate(Selected, "CheckIn", today);
        }

        // --- Async initial load (UI-thread safe) ---

        private async Task LoadInitialAsync()
        {
            try
            {
                var list = await _bookings.ListAsync(CancellationToken.None);
                if (list?.Any() == true)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Results.Clear();
                        foreach (var b in list) Results.Add(b);
                    });
                    return;
                }
            }
            catch
            {
                // fall through to local mock
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Results.Clear();
                Results.Add(Mock("BK-100241", "Alex Chen", "1204", DateTime.Today, DateTime.Today.AddDays(3)));
                Results.Add(Mock("BK-100242", "Priya N.", "0711", DateTime.Today, DateTime.Today.AddDays(2)));
                Results.Add(Mock("BK-100243", "J. Morales", "1502", DateTime.Today, DateTime.Today.AddDays(5)));
            });
        }

        // --- Helpers ---

        private static bool Contains(string? haystack, string needle)
            => haystack?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string? GetString(object obj, string prop)
        {
            var pi = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (pi is null) return null;
            var val = pi.GetValue(obj);
            return val switch
            {
                null => null,
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => val.ToString()
            };
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
                    else if (destType == typeof(int) && value is string ss && int.TryParse(ss, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv))
                        converted = iv;
                    else
                        converted = Convert.ChangeType(value, destType, CultureInfo.InvariantCulture);
                }

                pi.SetValue(target, converted);
            }
            catch
            {
                // ignored by design
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
                    var t = Type.GetType("System.DateOnly");
                    if (t != null)
                    {
                        var ctor = t.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
                        var dateOnly = ctor?.Invoke(new object[] { dt.Year, dt.Month, dt.Day });
                        if (dateOnly != null) pi.SetValue(target, dateOnly);
                    }
                }
                else if (pi.PropertyType == typeof(DateTime))
                {
                    pi.SetValue(target, dt);
                }
            }
            catch
            {
                // ignored
            }
        }

        private static Booking Mock(string id, string guest, string room, DateTime ci, DateTime co)
        {
            var b = Activator.CreateInstance<Booking>()
#pragma warning disable SYSLIB0050
                    ?? (Booking)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Booking));
#pragma warning restore SYSLIB0050

            TrySet(b, "BookingId", id);
            TrySet(b, "GuestName", guest);

            if (int.TryParse(room, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rn))
                TrySet(b, "RoomNumber", rn);
            else
                TrySet(b, "RoomNumber", room);

            TrySetDate(b, "CheckIn", ci);
            TrySetDate(b, "CheckOut", co);
            return b;
        }

        private void Raise(string propertyName)
        {
            try
            {
                var mi = GetType().GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                         ?? typeof(ViewModelBase).GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                mi?.Invoke(this, new object?[] { propertyName });
            }
            catch { /* ignored */ }
        }
    }
}
