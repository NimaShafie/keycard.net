// ViewModels/SettingsViewModel.cs
using System;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input; // RelayCommand / AsyncRelayCommand

using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels
{
    /// <summary>
    /// App settings (mock mode, refresh, theme).
    /// Note: We avoid [ObservableProperty] here to prevent conflicts with ViewModelBase's INotifyPropertyChanged.
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IAppEnvironment _env;

        // ----- Backing fields -----
        private bool _useMocks;
        private bool _enableSounds;
        private bool _autoRefresh;
        private int _refreshIntervalSeconds = 30;
        private string _theme = "Dark";
        private string? _statusMessage;

        // ----- Constants / Limits -----
        public const int MinRefreshSeconds = 5;
        public const int MaxRefreshSeconds = 300;

        // ----- Computed / environment-backed info (one-way bindings) -----
        public string CurrentMode => _env.IsMock ? "Mock Mode" : "Live Mode";
        public string ApiBaseUrl => _env.ApiBaseUrl;
        public string Environment => _env.EnvironmentName;

        // ----- Public properties (manual change notification) -----
        public bool UseMocks
        {
            get => _useMocks;
            set
            {
                if (_useMocks == value) return;
                _useMocks = value;
                OnPropertyChanged(nameof(UseMocks));
                StatusMessage = value
                    ? "⚠️ Mock mode selected (requires app restart)"
                    : "⚠️ Live mode selected (requires app restart)";
            }
        }

        public bool EnableSounds
        {
            get => _enableSounds;
            set
            {
                if (_enableSounds == value) return;
                _enableSounds = value;
                OnPropertyChanged(nameof(EnableSounds));
            }
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                if (_autoRefresh == value) return;
                _autoRefresh = value;
                OnPropertyChanged(nameof(AutoRefresh));
            }
        }

        public int RefreshIntervalSeconds
        {
            get => _refreshIntervalSeconds;
            set
            {
                var clamped = Math.Min(Math.Max(value, MinRefreshSeconds), MaxRefreshSeconds);
                if (_refreshIntervalSeconds == clamped) return;

                _refreshIntervalSeconds = clamped;
                OnPropertyChanged(nameof(RefreshIntervalSeconds));

                if (value < MinRefreshSeconds)
                {
                    StatusMessage = $"Refresh interval increased to minimum of {MinRefreshSeconds}s.";
                }
                else if (value > MaxRefreshSeconds)
                {
                    StatusMessage = $"Refresh interval reduced to maximum of {MaxRefreshSeconds}s.";
                }
            }
        }

        public string Theme
        {
            get => _theme;
            set
            {
                if (_theme == value) return;
                _theme = value;
                OnPropertyChanged(nameof(Theme));
            }
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        // ----- Commands -----
        public IAsyncRelayCommand SaveSettingsCommand { get; }
        public ICommand ResetToDefaultsCommand { get; }

        // ----- ctor -----
        public SettingsViewModel(IAppEnvironment env)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));

            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            ResetToDefaultsCommand = new RelayCommand(ResetToDefaults);

            LoadSettings();
        }

        // ----- Init / Load -----
        private void LoadSettings()
        {
            // In a real app these would be loaded from persisted user preferences.
            UseMocks = _env.IsMock;
            EnableSounds = false;
            AutoRefresh = true;
            RefreshIntervalSeconds = 30;
            Theme = "Dark";
        }

        // ----- Command handlers -----
        private async Task SaveSettingsAsync()
        {
            try
            {
                StatusMessage = "Saving settings...";

                // TODO: Persist values to a settings store (file/registry/db).
                await Task.Delay(500);

                StatusMessage = "Settings saved successfully";

                // Clear message after a short delay
                await Task.Delay(3000);
                StatusMessage = null;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving settings: {ex.Message}";
            }
        }

        private void ResetToDefaults()
        {
            EnableSounds = false;
            AutoRefresh = true;
            RefreshIntervalSeconds = 30;
            Theme = "Dark";
            StatusMessage = "Settings reset to defaults";
        }
    }
}
