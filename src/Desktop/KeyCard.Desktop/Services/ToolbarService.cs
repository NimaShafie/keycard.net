// Services/ToolbarService.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.Services
{
    internal sealed class ToolbarService : IToolbarService
    {
        private readonly INavigationService _nav;

        private Func<Task>? _onRefreshAsync;
        private Action<string?>? _onSearch;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ToolbarService(INavigationService nav)
        {
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));

            // Nav commands are **ALWAYS** enabled
            NavigateDashboardCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<ViewModels.DashboardViewModel>());
            NavigateFrontDeskCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<ViewModels.FrontDeskViewModel>());
            NavigateHousekeepingCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<ViewModels.HousekeepingViewModel>());
            NavigateFolioCommand = new UnifiedRelayCommand(() => _nav.NavigateTo<Modules.Folio.ViewModels.FolioViewModel>());

            // Page action commands: enabled only when context is attached
            ExecuteSearchCommand = new UnifiedRelayCommand(() => _onSearch?.Invoke(SearchText), () => _onSearch != null);
            ExecuteRefreshCommand = new UnifiedRelayCommand(async () => await DoRefreshAsync(), () => _onRefreshAsync != null && !IsRefreshing);
        }

        // ====== Properties ======
        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => Set(ref _isVisible, value); }

        private string? _title;
        public string? Title { get => _title; set => Set(ref _title, value); }

        private string? _subtitle;
        public string? Subtitle { get => _subtitle; set => Set(ref _subtitle, value); }

        private string? _searchText;
        public string? SearchText { get => _searchText; set => Set(ref _searchText, value); }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (Set(ref _isRefreshing, value))
                    (ExecuteRefreshCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private bool _isSyncPopupOpen;
        public bool IsSyncPopupOpen { get => _isSyncPopupOpen; set => Set(ref _isSyncPopupOpen, value); }

        private string? _syncMessage;
        public string? SyncMessage { get => _syncMessage; set => Set(ref _syncMessage, value); }

        // ====== Commands ======
        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateFrontDeskCommand { get; }
        public ICommand NavigateHousekeepingCommand { get; }
        public ICommand NavigateFolioCommand { get; }

        public ICommand ExecuteSearchCommand { get; }
        public ICommand ExecuteRefreshCommand { get; }

        // ====== Context wiring ======
        public void AttachContext(
            string title,
            string? subtitle,
            Func<Task>? onRefreshAsync,
            Action<string?>? onSearch,
            string? initialSearchText = null)
        {
            Title = title;
            Subtitle = subtitle;
            _onRefreshAsync = onRefreshAsync;
            _onSearch = onSearch;
            SearchText = initialSearchText;

            // Show toolbar and enable page actions
            IsVisible = true;
            (ExecuteSearchCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            (ExecuteRefreshCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
        }

        public void DetachContext()
        {
            _onRefreshAsync = null;
            _onSearch = null;
            Title = null;
            Subtitle = null;
            SearchText = null;
            IsRefreshing = false;
            IsSyncPopupOpen = false;
            SyncMessage = null;

            (ExecuteSearchCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            (ExecuteRefreshCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
        }

        private async Task DoRefreshAsync()
        {
            if (_onRefreshAsync == null || IsRefreshing) return;

            try
            {
                IsRefreshing = true;
                IsSyncPopupOpen = true;
                SyncMessage = "Syncing...";

                await _onRefreshAsync.Invoke();

                SyncMessage = "✓ Synced";
                await Task.Delay(1200);
            }
            catch
            {
                SyncMessage = "✗ Sync failed";
                await Task.Delay(1400);
            }
            finally
            {
                IsRefreshing = false;
                IsSyncPopupOpen = false;
                SyncMessage = null;
            }
        }

        // ====== INotifyPropertyChanged helper ======
        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value!;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }
}
