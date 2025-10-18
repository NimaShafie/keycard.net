// ViewModels/FrontDeskViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    public partial class FrontDeskViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private readonly IBookingService _bookings;
        private readonly IRoomsService _rooms;

        private Booking? _selected;
        public Booking? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    (CheckInCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (CheckOutCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AssignRoomCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                    OnPropertyChanged(nameof(Query));
                }
            }
        }

        public string? Query
        {
            get => SearchText;
            set
            {
                if (!string.Equals(_searchText, value, StringComparison.Ordinal))
                {
                    SearchText = value;
                    OnPropertyChanged(nameof(Query));
                }
            }
        }

        private string? _roomNumberInput;
        public string? RoomNumberInput
        {
            get => _roomNumberInput;
            set
            {
                if (SetProperty(ref _roomNumberInput, value))
                    (AssignRoomCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string? _assignRoomError;
        public string? AssignRoomError
        {
            get => _assignRoomError;
            private set => SetProperty(ref _assignRoomError, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    (RefreshCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (SearchCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (CheckInCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (CheckOutCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AssignRoomCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<Booking> Arrivals { get; } = new();
        public ObservableCollection<Booking> Departures { get; } = new();
        public ObservableCollection<Booking> Results { get; } = new();

        private readonly ObservableCollection<Booking> _allArrivals = new();
        private readonly ObservableCollection<Booking> _allDepartures = new();

        public ICommand BackToDashboardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }
        public ICommand AssignRoomCommand { get; }

        public FrontDeskViewModel(INavigationService nav, IBookingService bookings, IRoomsService rooms)
        {
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _bookings = bookings ?? throw new ArgumentNullException(nameof(bookings));
            _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));

            BackToDashboardCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<DashboardViewModel>());
            RefreshCommand = new UnifiedRelayCommand(RefreshAsync, () => !IsBusy);
            SearchCommand = new UnifiedRelayCommand(param => SearchAsync(param as string), _ => !IsBusy);
            CheckInCommand = new UnifiedRelayCommand(CheckInAsync, () => CanCheckIn());
            CheckOutCommand = new UnifiedRelayCommand(CheckOutAsync, () => CanCheckOut());
            AssignRoomCommand = new UnifiedRelayCommand(AssignRoomAsync, () => CanAssignRoom());

            _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading bookings...";

                // Pull bookings
                var arrivals = await _bookings.GetTodayArrivalsAsync();
                var allBookings = await _bookings.ListAsync();
                var today = DateOnly.FromDateTime(DateTime.Today);
                var departures = allBookings.Where(b => b.CheckOutDate == today).ToList();

                _allArrivals.Clear();
                foreach (var a in arrivals) _allArrivals.Add(a);

                _allDepartures.Clear();
                foreach (var d in departures) _allDepartures.Add(d);

                Arrivals.Clear();
                foreach (var a in arrivals) Arrivals.Add(a);

                Departures.Clear();
                foreach (var d in departures) Departures.Add(d);

                ApplyFilter();
                StatusMessage = $"Loaded {arrivals.Count} arrivals, {departures.Count} departures";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchAsync(string? query)
        {
            if (query is not null) Query = query;

            if (string.IsNullOrWhiteSpace(Query))
            {
                ApplyFilter();
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"Searching for '{Query}'...";

                var booking = await _bookings.FindBookingByCodeAsync(Query);

                if (booking is not null)
                {
                    Results.Clear();
                    Results.Add(booking);
                    Selected = booking;
                    StatusMessage = "Found booking";
                }
                else
                {
                    ApplyFilter();
                    StatusMessage = Results.Count == 0
                        ? "No bookings found"
                        : $"Found {Results.Count} booking(s)";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static bool Overlaps(DateOnly aStart, DateOnly aEnd, DateOnly bStart, DateOnly bEnd)
        {
            // Checkout is exclusive
            return aStart < bEnd && bStart < aEnd;
        }

        private bool IsRoomAvailableFor(Booking target, int roomNumber)
        {
            // Check against what we currently have loaded in the view
            var all = Results.Concat(Arrivals).Concat(Departures).Distinct();

            foreach (var b in all)
            {
                if (b.BookingId == target.BookingId) continue;
                if (b.Status.Equals("CheckedOut", StringComparison.OrdinalIgnoreCase)) continue;
                if (b.RoomNumber != roomNumber) continue;

                if (Overlaps(target.CheckInDate, target.CheckOutDate, b.CheckInDate, b.CheckOutDate))
                    return false;
            }
            return true;
        }

        private async Task AssignRoomAsync()
        {
            if (Selected is null || string.IsNullOrWhiteSpace(RoomNumberInput)) return;

            if (!int.TryParse(RoomNumberInput, out var roomNumber))
            {
                AssignRoomError = "Invalid room number";
                StatusMessage = "Invalid room number";
                return;
            }

            // 1) Validate existence using live/mock rooms service
            bool exists;
            try
            {
                exists = await _rooms.ExistsAsync(roomNumber, CancellationToken.None);
            }
            catch
            {
                // If service fails, we fall back to allowing the assignment as long as there is no conflict.
                exists = true;
            }

            if (!exists)
            {
                AssignRoomError = $"Room {roomNumber} does not exist.";
                StatusMessage = AssignRoomError;
                return;
            }

            // 2) Local conflict detection
            if (!IsRoomAvailableFor(Selected, roomNumber))
            {
                AssignRoomError = $"Room {roomNumber} is already occupied for {Selected.CheckInDate:MM/dd}–{Selected.CheckOutDate:MM/dd}.";
                StatusMessage = AssignRoomError;
                return;
            }

            try
            {
                IsBusy = true;
                AssignRoomError = null;
                StatusMessage = $"Assigning room {roomNumber}...";

                var success = await _bookings.AssignRoomAsync(
                    Selected.ConfirmationCode,
                    roomNumber);

                if (success)
                {
                    var updated = Selected with { RoomNumber = roomNumber };
                    ReplaceBookingInCollections(Selected, updated);
                    Selected = updated;
                    RoomNumberInput = string.Empty;
                    StatusMessage = $"Room {roomNumber} assigned successfully";
                }
                else
                {
                    StatusMessage = "Failed to assign room";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanAssignRoom() =>
            !IsBusy &&
            Selected is not null &&
            !string.IsNullOrWhiteSpace(RoomNumberInput);

        private async Task CheckInAsync()
        {
            if (Selected is null) return;

            try
            {
                IsBusy = true;
                StatusMessage = $"Checking in {Selected.GuestName}...";

                var success = await _bookings.CheckInAsync(Selected.ConfirmationCode);

                if (success)
                {
                    var updated = Selected with { Status = "CheckedIn" };
                    ReplaceBookingInCollections(Selected, updated);
                    Selected = updated;
                    StatusMessage = $"{Selected.GuestName} checked in successfully";
                }
                else
                {
                    StatusMessage = "Check-in failed";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanCheckIn() =>
            !IsBusy &&
            Selected is not null &&
            Selected.Status != "CheckedIn";

        private async Task CheckOutAsync()
        {
            if (Selected is null) return;

            try
            {
                IsBusy = true;
                StatusMessage = $"Checking out {Selected.GuestName}...";

                await Task.Delay(500);

                var updated = Selected with { Status = "CheckedOut" };
                ReplaceBookingInCollections(Selected, updated);
                Selected = updated;
                StatusMessage = $"{Selected.GuestName} checked out successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanCheckOut() =>
            !IsBusy &&
            Selected is not null &&
            Selected.Status == "CheckedIn";

        private void ApplyFilter()
        {
            var term = (SearchText ?? string.Empty).Trim();
            var hasTerm = term.Length > 0;

            var filteredArrivals = hasTerm
                ? _allArrivals.Where(b => MatchesFilter(b, term))
                : _allArrivals;

            var filteredDepartures = hasTerm
                ? _allDepartures.Where(b => MatchesFilter(b, term))
                : _allDepartures;

            Arrivals.ReplaceWith(filteredArrivals);
            Departures.ReplaceWith(filteredDepartures);
            Results.ReplaceWith(filteredArrivals.Concat(filteredDepartures));

            if (Selected is not null && !Results.Contains(Selected))
            {
                Selected = null;
            }
        }

        private static bool MatchesFilter(Booking b, string term)
        {
            return b.BookingId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || b.GuestName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || b.RoomNumber.ToString(CultureInfo.InvariantCulture).Contains(term, StringComparison.OrdinalIgnoreCase)
                || b.ConfirmationCode.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private void ReplaceBookingInCollections(Booking old, Booking updated)
        {
            ReplaceInCollection(_allArrivals, old, updated);
            ReplaceInCollection(_allDepartures, old, updated);
            ReplaceInCollection(Arrivals, old, updated);
            ReplaceInCollection(Departures, old, updated);
            ReplaceInCollection(Results, old, updated);
        }

        private static void ReplaceInCollection(ObservableCollection<Booking> collection, Booking old, Booking updated)
        {
            var index = collection.IndexOf(old);
            if (index >= 0)
            {
                collection[index] = updated;
            }
        }
    }

    public static class ObservableCollectionSyncExtensions
    {
        public static void ReplaceWith<T>(this ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }
    }
}
