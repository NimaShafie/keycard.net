// ViewModels/FrontDeskViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Mocks;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    public class FrontDeskViewModel : ViewModelBase
    {
        private readonly INavigationService? _nav;

        // ---- Bindable properties ----

        private Booking? _selected;
        public Booking? Selected
        {
            get => _selected;
            set
            {
                if (!Equals(_selected, value))
                {
                    _selected = value;
                    OnPropertyChanged();
                    (CheckInCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CheckOutCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (_assignRoomCore as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplyFilter();
                    OnPropertyChanged(nameof(Query)); // keep alias in sync
                }
            }
        }

        /// <summary>Alias some XAML uses.</summary>
        public string? Query
        {
            get => SearchText;
            set
            {
                if (!string.Equals(_searchText, value, StringComparison.Ordinal))
                {
                    SearchText = value;               // triggers ApplyFilter
                    OnPropertyChanged(nameof(Query)); // explicit notify for alias
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (_assignRoomCore as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<Booking> Arrivals { get; } = new();
        public ObservableCollection<Booking> Departures { get; } = new();
        public ObservableCollection<Booking> Results { get; } = new();

        // Backing stores for filtering (not bound)
        private readonly ObservableCollection<Booking> _allArrivals = new();
        private readonly ObservableCollection<Booking> _allDepartures = new();

        // ---- Commands ----
        public ICommand BackToDashboardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }

        // Keep a single core command instance and expose both names for XAML compatibility.
        private readonly ICommand _assignRoomCore;
        /// <summary>Some XAML uses this name.</summary>
        public ICommand AssignRoomNumber => _assignRoomCore;
        /// <summary>Other XAML uses this name.</summary>
        public ICommand AssignRoomCommand => _assignRoomCore;

        // ---- Ctors ----
        public FrontDeskViewModel() : this(null) { }

        public FrontDeskViewModel(INavigationService? nav)
        {
            _nav = nav;

            BackToDashboardCommand = new RelayCommand(_ => _nav?.NavigateTo<DashboardViewModel>());
            RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsBusy);

            // Search: optional CommandParameter (string). If present, uses it; otherwise re-applies filter.
            SearchCommand = new RelayCommand(param =>
            {
                if (param is string s)
                    Query = s;   // sets SearchText and applies filter
                else
                    ApplyFilter();
            }, _ => !IsBusy);

            CheckInCommand = new RelayCommand(b =>
            {
                var booking = b as Booking ?? Selected;
                if (booking is null) return;
                // TODO: integrate with backend to mark check-in
            },
            _ => Selected is not null && !IsBusy);

            CheckOutCommand = new RelayCommand(b =>
            {
                var booking = b as Booking ?? Selected;
                if (booking is null) return;
                // TODO: integrate with backend to mark check-out
            },
            _ => Selected is not null && !IsBusy);

            // Core assign-room command used by both AssignRoomNumber and AssignRoomCommand properties.
            _assignRoomCore = new RelayCommand(b =>
            {
                var booking = b as Booking ?? Selected;
                if (booking is null) return;

                // TODO: choose/find room, call service, then update list item (immutable model considerations)
                // Example placeholder:
                // var newRoom = 500; // demo
                // var updated = booking with { RoomNumber = newRoom }; // if Booking is a record
                // ReplaceInCollections(updated);
            },
            _ => Selected is not null && !IsBusy);

            // ensure lists aren’t blank when the view opens
            _ = RefreshAsync();
        }

        // ---- Behaviors ----

        private async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                await Task.Delay(200); // simulate fetch

                var arrivals = KeyCard.Desktop.Mocks.BookingMocks.GetArrivalsToday(10);
                var departures = KeyCard.Desktop.Mocks.BookingMocks.GetDeparturesToday(5);

                // If you have backing stores (e.g., _allArrivals/_allDepartures), keep them in sync
                _allArrivals?.Clear();
                if (_allArrivals is not null) foreach (var a in arrivals) _allArrivals.Add(a);

                _allDepartures?.Clear();
                if (_allDepartures is not null) foreach (var d in departures) _allDepartures.Add(d);

                // Always update the bound collections the view uses
                Arrivals.Clear(); foreach (var a in arrivals) Arrivals.Add(a);
                Departures.Clear(); foreach (var d in departures) Departures.Add(d);

                ApplyFilter();
                Selected = null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var term = (SearchText ?? string.Empty).Trim();
            bool hasTerm = term.Length > 0;

            static bool Match(Booking b, string t) =>
                b.BookingId.ToString("D", CultureInfo.InvariantCulture).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (b.GuestName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                b.RoomNumber.ToString(CultureInfo.InvariantCulture).Contains(t, StringComparison.OrdinalIgnoreCase);

            var filteredArrivals = hasTerm ? _allArrivals.Where(b => Match(b, term)) : _allArrivals;
            var filteredDepartures = hasTerm ? _allDepartures.Where(b => Match(b, term)) : _allDepartures;

            Arrivals.ReplaceWith(filteredArrivals);
            Departures.ReplaceWith(filteredDepartures);
            Results.ReplaceWith(filteredArrivals.Concat(filteredDepartures));

            if (Selected is not null && !Results.Contains(Selected))
                Selected = null;
        }

        // ---- Minimal ICommand impl ----
        internal sealed class RelayCommand : ICommand
        {
            private readonly Func<object?, bool>? _canExecute;
            private readonly Action<object?>? _execSync;
            private readonly Func<object?, Task>? _execAsync;

            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            { _execSync = execute ?? throw new ArgumentNullException(nameof(execute)); _canExecute = canExecute; }

            public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
            { _execAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync)); _canExecute = canExecute; }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public async void Execute(object? parameter)
            {
                if (_execSync is not null) { _execSync(parameter); return; }
                if (_execAsync is not null) { await _execAsync(parameter); }
            }

            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // Public helper
    public static class ObservableCollectionSyncExtensions
    {
        public static void ReplaceWith<T>(this ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }
    }
}
