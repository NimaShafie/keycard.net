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
        private readonly IBookingStateService _bookingState;
        private readonly IRoomsService _rooms;
        private readonly IToolbarService _toolbar;
        private readonly IAppEnvironment? _env;

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

        // Mock mode indicator for UI
        public bool IsMockMode => _env?.IsMock ?? true;

        // ✅ Use the shared collection from BookingStateService
        public ObservableCollection<Booking> Arrivals => _bookingState.TodayArrivals;

        public ObservableCollection<Booking> Departures { get; } = new();
        public ObservableCollection<Booking> Results { get; } = new();

        private readonly ObservableCollection<Booking> _allDepartures = new();

        public ICommand BackToDashboardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }
        public ICommand AssignRoomCommand { get; }

        public FrontDeskViewModel(
            INavigationService nav,
            IBookingStateService bookingState,
            IRoomsService rooms,
            IToolbarService toolbar,
            IAppEnvironment? env = null)
        {
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _bookingState = bookingState ?? throw new ArgumentNullException(nameof(bookingState));
            _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            _toolbar = toolbar;
            _env = env;

            BackToDashboardCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<DashboardViewModel>());
            RefreshCommand = new UnifiedRelayCommand(RefreshAsync, () => !IsBusy);
            SearchCommand = new UnifiedRelayCommand(param => SearchAsync(param as string), _ => !IsBusy);
            CheckInCommand = new UnifiedRelayCommand(CheckInAsync, () => CanCheckIn());
            CheckOutCommand = new UnifiedRelayCommand(CheckOutAsync, () => CanCheckOut());
            AssignRoomCommand = new UnifiedRelayCommand(AssignRoomAsync, () => CanAssignRoom());

            _toolbar.AttachContext(
                title: "Front Desk Operations",
                subtitle: "Manage arrivals, departures, and check-ins",
                onRefreshAsync: RefreshAsync,
                onSearch: q => { SearchText = q ?? string.Empty; ApplyFilter(); },
                initialSearchText: SearchText
            );

            // ✅ Only refresh if data hasn't been loaded yet
            if (_bookingState.AllBookings.Count == 0)
            {
                _ = RefreshAsync();
            }
            else
            {
                // Data already loaded, just apply filter to display it
                ApplyFilter();
            }
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading bookings...";

                // ✅ Refresh the shared state
                await _bookingState.RefreshAsync();

                // Calculate departures locally
                var today = DateOnly.FromDateTime(DateTime.Today);
                var departures = _bookingState.AllBookings.Where(b => b.CheckOutDate == today).ToList();

                _allDepartures.Clear();
                foreach (var d in departures) _allDepartures.Add(d);

                Departures.Clear();
                foreach (var d in departures) Departures.Add(d);

                ApplyFilter();
                StatusMessage = $"Loaded {Arrivals.Count} arrivals, {departures.Count} departures";
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

                // ✅ Search in the shared state
                var booking = _bookingState.FindByCode(Query);

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

                var confirmationCode = Selected.ConfirmationCode;

                // ✅ Use the state service - it updates the shared collection
                var success = await _bookingState.AssignRoomAsync(confirmationCode, roomNumber);

                if (success)
                {
                    // ✅ CRITICAL: Refresh Results collection to show updated booking
                    ApplyFilter();

                    // ✅ CRITICAL: Update Selected to point to the new booking instance
                    Selected = _bookingState.FindByCode(confirmationCode);

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

                var confirmationCode = Selected.ConfirmationCode;

                // ✅ Use the state service - it updates the shared collection
                var success = await _bookingState.CheckInAsync(confirmationCode);

                if (success)
                {
                    // ✅ CRITICAL: Refresh Results collection to show updated booking
                    ApplyFilter();

                    // ✅ CRITICAL: Update Selected to point to the new booking instance
                    Selected = _bookingState.FindByCode(confirmationCode);

                    StatusMessage = $"{Selected?.GuestName ?? "Guest"} checked in successfully";
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

                var confirmationCode = Selected.ConfirmationCode;

                await Task.Delay(500);

                // ✅ Update the shared state directly
                _bookingState.UpdateBookingStatus(confirmationCode, "CheckedOut");

                // ✅ CRITICAL: Refresh Results collection to show updated booking
                ApplyFilter();

                // ✅ CRITICAL: Update Selected to point to the new booking instance
                Selected = _bookingState.FindByCode(confirmationCode);

                StatusMessage = $"{Selected?.GuestName ?? "Guest"} checked out successfully";
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
                ? Arrivals.Where(b => MatchesFilter(b, term))
                : Arrivals;

            var filteredDepartures = hasTerm
                ? _allDepartures.Where(b => MatchesFilter(b, term))
                : _allDepartures;

            Departures.ReplaceWith(filteredDepartures);
            Results.ReplaceWith(filteredArrivals.Concat(filteredDepartures));

            // ✅ CRITICAL: If Selected was updated, find the new instance in Results
            if (Selected is not null)
            {
                var updatedSelected = Results.FirstOrDefault(b => b.ConfirmationCode == Selected.ConfirmationCode);
                if (updatedSelected != null)
                {
                    // Don't trigger property change if it's the same instance
                    if (!ReferenceEquals(Selected, updatedSelected))
                    {
                        Selected = updatedSelected;
                    }
                }
                else if (!Results.Contains(Selected))
                {
                    Selected = null;
                }
            }
        }

        private static bool MatchesFilter(Booking b, string term)
        {
            return b.BookingId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || b.GuestName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || b.RoomNumber.ToString(CultureInfo.InvariantCulture).Contains(term, StringComparison.OrdinalIgnoreCase)
                || b.ConfirmationCode.Contains(term, StringComparison.OrdinalIgnoreCase);
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
