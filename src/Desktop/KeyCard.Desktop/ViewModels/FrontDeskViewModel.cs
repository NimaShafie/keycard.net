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
                    // Populate edit fields when a booking is selected
                    if (_selected != null)
                    {
                        EditFirstName = _selected.GuestFirstName;
                        EditLastName = _selected.GuestLastName;
                        EditRoomType = _selected.RoomType;
                        // Convert DateOnly to DateTime for the date pickers
                        EditCheckInDate = _selected.CheckInDate.ToDateTime(TimeOnly.MinValue);
                        EditCheckOutDate = _selected.CheckOutDate.ToDateTime(TimeOnly.MinValue);
                        ValidationError = null;
                    }
                    else
                    {
                        EditFirstName = null;
                        EditLastName = null;
                        EditRoomType = null;
                        EditCheckInDate = null;
                        EditCheckOutDate = null;
                        ValidationError = null;
                    }

                    (CheckInCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (CheckOutCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AssignRoomCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (SaveChangesCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
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

        // Editable fields for the selected booking
        private string? _editFirstName;
        public string? EditFirstName
        {
            get => _editFirstName;
            set
            {
                if (SetProperty(ref _editFirstName, value))
                {
                    ValidationError = null;
                    (SaveChangesCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _editLastName;
        public string? EditLastName
        {
            get => _editLastName;
            set
            {
                if (SetProperty(ref _editLastName, value))
                {
                    ValidationError = null;
                    (SaveChangesCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _editRoomType;
        public string? EditRoomType
        {
            get => _editRoomType;
            set
            {
                if (SetProperty(ref _editRoomType, value))
                {
                    ValidationError = null;
                    (SaveChangesCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private DateTime? _editCheckInDate;
        public DateTime? EditCheckInDate
        {
            get => _editCheckInDate;
            set
            {
                if (SetProperty(ref _editCheckInDate, value))
                {
                    ValidateDates();
                    (SaveChangesCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private DateTime? _editCheckOutDate;
        public DateTime? EditCheckOutDate
        {
            get => _editCheckOutDate;
            set
            {
                if (SetProperty(ref _editCheckOutDate, value))
                {
                    ValidateDates();
                    (SaveChangesCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _validationError;
        public string? ValidationError
        {
            get => _validationError;
            set => SetProperty(ref _validationError, value);
        }

        // Available room types from backend
        public ObservableCollection<string> RoomTypes { get; } = new()
        {
            "Regular Room",
            "King Room",
            "Luxury Room"
        };

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
                    (SaveChangesCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
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
        public ICommand SaveChangesCommand { get; }

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
            SaveChangesCommand = new UnifiedRelayCommand(SaveChangesAsync, () => CanSaveChanges());

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

            // In mock mode, allow any room between 1-1000
            if (IsMockMode)
            {
                if (roomNumber < 1 || roomNumber > 1000)
                {
                    AssignRoomError = $"Room number must be between 1 and 1000.";
                    StatusMessage = AssignRoomError;
                    return;
                }
                exists = true; // Accept any room in valid range for mock mode
            }
            else
            {
                // In live mode, validate room existence via the rooms service
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

        private void ValidateDates()
        {
            if (EditCheckInDate.HasValue && EditCheckOutDate.HasValue)
            {
                if (EditCheckInDate.Value >= EditCheckOutDate.Value)
                {
                    ValidationError = "Check-out date must be after check-in date";
                }
                else
                {
                    ValidationError = null;
                }
            }
            else
            {
                ValidationError = null;
            }
        }

        private bool CanSaveChanges()
        {
            if (IsBusy || Selected == null) return false;

            // Convert DateTime? to DateOnly for comparison
            DateOnly? editCheckInDateOnly = EditCheckInDate.HasValue
                ? DateOnly.FromDateTime(EditCheckInDate.Value)
                : null;
            DateOnly? editCheckOutDateOnly = EditCheckOutDate.HasValue
                ? DateOnly.FromDateTime(EditCheckOutDate.Value)
                : null;

            // Check if any field has changed
            bool hasChanges =
                EditFirstName != Selected.GuestFirstName ||
                EditLastName != Selected.GuestLastName ||
                EditRoomType != Selected.RoomType ||
                editCheckInDateOnly != Selected.CheckInDate ||
                editCheckOutDateOnly != Selected.CheckOutDate;

            // Check if there are validation errors
            bool isValid = string.IsNullOrEmpty(ValidationError);

            // Must have at least first or last name
            bool hasName = !string.IsNullOrWhiteSpace(EditFirstName) || !string.IsNullOrWhiteSpace(EditLastName);

            // Must have valid dates
            bool hasDates = EditCheckInDate.HasValue && EditCheckOutDate.HasValue;

            return hasChanges && isValid && hasName && hasDates;
        }

        private async Task SaveChangesAsync()
        {
            if (Selected is null || !EditCheckInDate.HasValue || !EditCheckOutDate.HasValue) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Saving changes...";

                var confirmationCode = Selected.ConfirmationCode;

                // Create updated booking with new values (convert DateTime back to DateOnly)
                var updatedBooking = Selected with
                {
                    GuestFirstName = EditFirstName?.Trim() ?? "",
                    GuestLastName = EditLastName?.Trim() ?? "",
                    RoomType = EditRoomType ?? "Regular Room",
                    CheckInDate = DateOnly.FromDateTime(EditCheckInDate.Value),
                    CheckOutDate = DateOnly.FromDateTime(EditCheckOutDate.Value)
                };

                // In mock mode, directly update the shared collection
                // Find the booking in all collections and replace it
                var allBookings = _bookingState.AllBookings;
                var arrivals = _bookingState.TodayArrivals;

                // Find index in AllBookings
                var indexInAll = -1;
                for (int i = 0; i < allBookings.Count; i++)
                {
                    if (allBookings[i].ConfirmationCode == confirmationCode)
                    {
                        indexInAll = i;
                        break;
                    }
                }

                // Find index in TodayArrivals
                var indexInArrivals = -1;
                for (int i = 0; i < arrivals.Count; i++)
                {
                    if (arrivals[i].ConfirmationCode == confirmationCode)
                    {
                        indexInArrivals = i;
                        break;
                    }
                }

                // Replace in both collections
                if (indexInAll >= 0)
                {
                    allBookings[indexInAll] = updatedBooking;
                }

                if (indexInArrivals >= 0)
                {
                    arrivals[indexInArrivals] = updatedBooking;
                }

                // Also update departures if needed
                if (updatedBooking.CheckOutDate == DateOnly.FromDateTime(DateTime.Today))
                {
                    var indexInDepartures = -1;
                    for (int i = 0; i < _allDepartures.Count; i++)
                    {
                        if (_allDepartures[i].ConfirmationCode == confirmationCode)
                        {
                            indexInDepartures = i;
                            break;
                        }
                    }

                    if (indexInDepartures >= 0)
                    {
                        _allDepartures[indexInDepartures] = updatedBooking;
                    }
                }

                await Task.Delay(300); // Simulate save delay

                // Refresh the display
                ApplyFilter();

                // Update Selected to point to the new booking instance
                Selected = _bookingState.FindByCode(confirmationCode);

                StatusMessage = "Changes saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                ValidationError = $"Failed to save: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

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
