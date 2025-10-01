// ViewModels/FrontDeskViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;

namespace KeyCard.Desktop.ViewModels
{
    public sealed class FrontDeskViewModel : ViewModelBase
    {
        private string _query = string.Empty;
        public string Query
        {
            get => _query;
            set
            {
                if (SetProperty(ref _query, value))
                    (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string? _selected;
        public string? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    (CheckInCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (AssignRoomCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _assignRoomNumber = string.Empty;
        public string AssignRoomNumber
        {
            get => _assignRoomNumber;
            set
            {
                if (SetProperty(ref _assignRoomNumber, value))
                    (AssignRoomCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<string> Results { get; } = new();

        public ICommand SearchCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand AssignRoomCommand { get; }

        public FrontDeskViewModel()
        {
            SearchCommand = new RelayCommand(
                execute: () => _ = SearchAsync(),
                canExecute: () => !string.IsNullOrWhiteSpace(Query)
            );

            CheckInCommand = new RelayCommand(
                execute: () => _ = CheckInAsync(),
                canExecute: () => Selected is not null
            );

            AssignRoomCommand = new RelayCommand(
                execute: () => _ = AssignRoomAsync(),
                canExecute: () => Selected is not null && !string.IsNullOrWhiteSpace(AssignRoomNumber)
            );
        }

        private async Task SearchAsync()
        {
            // TODO: call your backend search; this stub just returns a few items.
            await Task.Yield();
            Results.Clear();
            Results.Add($"Booking for {Query} — #A123");
            Results.Add($"Booking for {Query} — #B456");
            Results.Add($"Booking for {Query} — #C789");
            // Optionally auto-select first
            Selected = Results.Count > 0 ? Results[0] : null;
        }

        private async Task CheckInAsync()
        {
            if (Selected is null) return;
            // TODO: call backend to check-in the selected booking
            await Task.Yield();
            // Example: mark item as checked-in in UI, show toast, or refresh list
        }

        private async Task AssignRoomAsync()
        {
            if (Selected is null || string.IsNullOrWhiteSpace(AssignRoomNumber)) return;
            // TODO: call backend to assign room number to booking
            await Task.Yield();
            // Example: update UI, clear the input
            AssignRoomNumber = string.Empty;
        }
    }
}
