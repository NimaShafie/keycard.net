// ViewModels/DashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Modules.Folio.ViewModels;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private readonly IBookingService _bookings;
        private readonly IAuthService _auth;
        private readonly IToolbarService? _toolbar;

        private string? _currentUserDisplay;
        public string? CurrentUserDisplay
        {
            get => _currentUserDisplay;
            set => SetProperty(ref _currentUserDisplay, value);
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilter();
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (SetProperty(ref _isRefreshing, value))
                    (RefreshCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string? _syncMessage;
        public string? SyncMessage
        {
            get => _syncMessage;
            set => SetProperty(ref _syncMessage, value);
        }

        public ObservableCollection<Booking> Arrivals { get; } = new();
        public ObservableCollection<Booking> FilteredArrivals { get; } = new();

        public ICommand GoFrontDeskCommand { get; }
        public ICommand GoHousekeepingCommand { get; }
        public ICommand GoFolioCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }

        public DashboardViewModel(
            INavigationService nav,
            IBookingService bookings,
            IAuthService auth,
            IToolbarService? toolbar = null)
        {
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _bookings = bookings ?? throw new ArgumentNullException(nameof(bookings));
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _toolbar = toolbar;

            CurrentUserDisplay = _auth.DisplayName ?? "Staff Console";

            GoFrontDeskCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<FrontDeskViewModel>());
            GoHousekeepingCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<HousekeepingViewModel>());
            GoFolioCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<FolioViewModel>());

            RefreshCommand = new UnifiedRelayCommand(RefreshAsync, () => !IsRefreshing);
            SearchCommand = new UnifiedRelayCommand(() => ApplyFilter());

            // Configure the shared toolbar if available
            if (_toolbar != null)
            {
                _toolbar.AttachContext(
                    title: "Dashboard Overview",
                    subtitle: "Mock Staff",
                    onRefreshAsync: RefreshAsync,
                    onSearch: q =>
                    {
                        SearchText = q ?? string.Empty;
                        ApplyFilter();
                    },
                    initialSearchText: SearchText
                );
            }

            _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (IsRefreshing) return;

            try
            {
                IsRefreshing = true;

                var arrivals = await _bookings.GetTodayArrivalsAsync();

                Arrivals.Clear();
                foreach (var booking in arrivals)
                {
                    Arrivals.Add(booking);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to refresh arrivals: {ex.Message}");
                Arrivals.Clear();
                FilteredArrivals.Clear();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void ApplyFilter()
        {
            var term = (SearchText ?? string.Empty).Trim();

            FilteredArrivals.Clear();

            var filtered = string.IsNullOrWhiteSpace(term)
                ? Arrivals
                : Arrivals.Where(b => MatchesFilter(b, term));

            foreach (var booking in filtered)
            {
                FilteredArrivals.Add(booking);
            }
        }

        private static bool MatchesFilter(Booking booking, string term)
        {
            return booking.BookingId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || booking.GuestName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || booking.RoomNumber.ToString(CultureInfo.InvariantCulture).Contains(term, StringComparison.OrdinalIgnoreCase)
                || booking.ConfirmationCode.Contains(term, StringComparison.OrdinalIgnoreCase);
        }
    }
}
