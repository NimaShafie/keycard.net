// Services/IToolbarService.cs
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KeyCard.Desktop.Services
{
    public interface IToolbarService : INotifyPropertyChanged
    {
        // Visibility
        bool IsVisible { get; set; }

        // Text
        string? Title { get; set; }
        string? Subtitle { get; set; }
        string? SearchText { get; set; }

        // Status
        bool IsRefreshing { get; set; }
        bool IsSyncPopupOpen { get; set; }
        string? SyncMessage { get; set; }

        // Navigation (always available)
        ICommand NavigateDashboardCommand { get; }
        ICommand NavigateFrontDeskCommand { get; }
        ICommand NavigateHousekeepingCommand { get; }
        ICommand NavigateFolioCommand { get; }

        // Page actions (enabled when a page attaches)
        ICommand ExecuteSearchCommand { get; }
        ICommand ExecuteRefreshCommand { get; }

        // Context wiring from pages
        void AttachContext(
            string title,
            string? subtitle,
            Func<Task>? onRefreshAsync,
            Action<string?>? onSearch,
            string? initialSearchText = null);

        void DetachContext();
    }
}
