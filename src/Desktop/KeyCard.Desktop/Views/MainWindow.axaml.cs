// Views/MainWindow.axaml.cs
using System;
using System.ComponentModel;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();     // Avalonia-generated hookup
            TrySetWindowIcon();

            this.AttachedToVisualTree += (_, __) => WireTopBar();
        }

        private void WireTopBar()
        {
            // Find the TopBar from XAML
            var topBar = this.FindControl<Control>("AppTopBar");
            if (topBar is null) return;

            // Resolve IToolbarService from App.Services if available
            IToolbarService? toolbar = null;
            try
            {
                var app = Avalonia.Application.Current;
                var servicesProp = app?.GetType().GetProperty("Services", BindingFlags.Public | BindingFlags.Instance);
                if (servicesProp?.GetValue(app) is IServiceProvider sp)
                    toolbar = sp.GetService(typeof(IToolbarService)) as IToolbarService;
            }
            catch { /* ignore */ }

            // If resolved, set DataContext so commands are live (no more grey buttons)
            if (toolbar is not null)
                topBar.DataContext = toolbar;

            // React when the current view changes to hide the TopBar on Login
            if (DataContext is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Current")
                        UpdateTopBarVisibility(topBar, toolbar);
                };
            }

            // initial
            UpdateTopBarVisibility(topBar, toolbar);
        }

        private void UpdateTopBarVisibility(Control topBar, IToolbarService? toolbar)
        {
            var currentProp = DataContext?.GetType().GetProperty("Current");
            var currentVm = currentProp?.GetValue(DataContext, null);
            var isLogin = currentVm?.GetType().Name?.Equals("LoginViewModel", StringComparison.Ordinal) == true;

            // Set control visibility (visual guarantee)
            topBar.IsVisible = !isLogin;

            // Also set the service flag if available (logical state)
            if (toolbar is not null)
                toolbar.IsVisible = !isLogin;
        }

        // Prevent clicks on modal content from closing the modal
        private void OnModalContentPressed(object? sender, PointerPressedEventArgs e)
        {
            // Stop event from bubbling up to the overlay button
            e.Handled = true;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void TrySetWindowIcon()
        {
            // Prefer Branding/KeyCard.ico; fall back to Assets/KeyCard.ico
            try
            {
                var uri = new Uri("avares://KeyCard.Desktop/Assets/Branding/KeyCard.ico");
                using var s = AssetLoader.Open(uri);
                Icon = new WindowIcon(s);
                return;
            }
            catch { /* continue */ }

            try
            {
                var uri2 = new Uri("avares://KeyCard.Desktop/Assets/KeyCard.ico");
                using var s2 = AssetLoader.Open(uri2);
                Icon = new WindowIcon(s2);
            }
            catch
            {
                // Swallow; app still runs with default icon.
            }
        }
    }
}
