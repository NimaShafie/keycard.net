// Views/Shared/TopBar.axaml.cs
using System;
using System.ComponentModel;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.Views.Shared
{
    public partial class TopBar : UserControl
    {
        public TopBar()
        {
            InitializeComponent();

            // Give this control a known name so MainWindow can find it for visibility toggling.
            this.Name = "__KeyCardTopBar__";

            // Resolve the toolbar service as reliably as possible and set DataContext
            var toolbar = TryResolveToolbar();
            if (toolbar is not null)
                DataContext = toolbar;

            // Also react to view changes for login visibility if the window didn’t set it yet.
            this.AttachedToVisualTree += (_, __) =>
            {
                var win = this.GetVisualRoot() as Window;
                if (win?.DataContext is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == "Current")
                            UpdateOwnVisibility(win);
                    };
                }
                UpdateOwnVisibility(win);
            };
        }

        private static IToolbarService? TryResolveToolbar()
        {
            // 1) Preferred: Application.Current.Services provider (no IHaveServiceProvider dependency)
            try
            {
                var app = Application.Current;
                var servicesProp = app?.GetType().GetProperty("Services", BindingFlags.Public | BindingFlags.Instance);
                var sp = servicesProp?.GetValue(app);
                if (sp is IServiceProvider provider)
                {
                    var svc = provider.GetService(typeof(IToolbarService)) as IToolbarService;
                    if (svc is not null) return svc;
                }
            }
            catch { /* ignore */ }

            // 2) Fallback: look for a public property "Toolbar" on the MainWindow's DataContext
            try
            {
                var lifetime = Application.Current?.ApplicationLifetime;
                if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desk)
                {
                    var win = desk.MainWindow;
                    var vm = win?.DataContext;
                    var p = vm?.GetType().GetProperty("Toolbar", BindingFlags.Public | BindingFlags.Instance);
                    if (p?.GetValue(vm) is IToolbarService t) return t;
                }
            }
            catch { /* ignore */ }

            return null;
        }

        private void UpdateOwnVisibility(Window? win)
        {
            if (win?.DataContext is null) return;

            var current = win.DataContext.GetType().GetProperty("Current")?.GetValue(win.DataContext, null);
            var isLogin = current?.GetType().Name?.Equals("LoginViewModel", StringComparison.Ordinal) == true;

            // If the host VM does not expose toolbar visibility, at least hide the control itself on login.
            this.IsVisible = !isLogin;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
