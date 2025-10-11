// ViewModels/DashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Mocks;

namespace KeyCard.Desktop.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly INavigationService? _nav;

        // ---- Bindable properties ----

        private string? _currentUserDisplay;
        public string? CurrentUserDisplay
        {
            get => _currentUserDisplay;
            set
            {
                if (_currentUserDisplay != value)
                {
                    _currentUserDisplay = value;
                    OnPropertyChanged();
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
                    // Optional: trigger filtering here if implemented
                }
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    OnPropertyChanged();
                    (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Collection bound to the "Today's Arrivals" grid.
        /// </summary>
        public ObservableCollection<Booking> Arrivals { get; } = new();

        // ---- Commands ----

        public ICommand GoFrontDeskCommand { get; }
        public ICommand GoHousekeepingCommand { get; }
        public ICommand RefreshCommand { get; }

        // ---- Constructors ----

        public DashboardViewModel()
            : this(nav: null)
        {
        }

        public DashboardViewModel(INavigationService? nav)
        {
            _nav = nav;

            // Set a sensible default; replace with your auth/env source if available.
            CurrentUserDisplay = "Staff Console";

            GoFrontDeskCommand = new RelayCommand(_ => OnNavigateFrontDesk());
            GoHousekeepingCommand = new RelayCommand(_ => OnNavigateHousekeeping());
            RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsRefreshing);

            // Optional initial load
            _ = RefreshAsync();
        }

        // ---- Behaviors ----

        private void OnNavigateFrontDesk()
        {
            // Navigate if a nav service is provided
            _nav?.NavigateTo<FrontDeskViewModel>();
        }

        private void OnNavigateHousekeeping()
        {
            _nav?.NavigateTo<HousekeepingViewModel>();
        }

        private async Task RefreshAsync()
        {
            if (IsRefreshing) return;
            try
            {
                IsRefreshing = true;
                await Task.Delay(250);
                var arrivals = BookingMocks.GetArrivalsToday(12);
                Arrivals.Clear();
                foreach (var b in arrivals) Arrivals.Add(b);
            }
            catch (Exception)
            {
                Arrivals.Clear(); // keep UI stable
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        // ---- Minimal ICommand implementation ----
        private sealed class RelayCommand : ICommand
        {
            private readonly Func<object?, bool>? _canExecute;
            private readonly Action<object?>? _executeSync;
            private readonly Func<object?, Task>? _executeAsync;

            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _executeSync = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
            {
                _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public async void Execute(object? parameter)
            {
                if (_executeSync is not null)
                {
                    _executeSync(parameter);
                    return;
                }

                if (_executeAsync is not null)
                {
                    await _executeAsync(parameter);
                }
            }

            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
