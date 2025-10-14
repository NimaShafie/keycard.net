// /App.axaml.cs
using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using KeyCard.Desktop.Services;
using KeyCard.Desktop.ViewModels;
using KeyCard.Desktop.Views;

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private Exception? _xamlLoadError;

    public App(IServiceProvider services)
        => _services = services ?? throw new ArgumentNullException(nameof(services));

    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (Exception ex)
        {
            _xamlLoadError = ex;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "keycard_desktop.log"),
                $"OnFrameworkInitializationCompleted: lifetime={ApplicationLifetime?.GetType().FullName}{Environment.NewLine}");
        }
        catch { }

        // Desktop lifetime (Windows/macOS/Linux)
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1) create + show a window so lifetime stays alive
            var main = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Title = "KeyCard.NET — Staff Console"
            };

            // Resolve env if registered; otherwise fall back to a default instance.
            var env = TryResolve<IAppEnvironment>();
            var modeLabel = (env?.IsMock ?? true) ? "MOCK" : "LIVE";
            main.Title = $"KeyCard.NET — Staff Console [{modeLabel}]";
            desktop.MainWindow = main;
            main.Show();
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // 2) wire DI + VM (with your existing safety net)
            try
            {
                if (_xamlLoadError is not null)
                    throw new InvalidOperationException("App XAML failed to load.", _xamlLoadError);

                var shell = SafeResolve<MainViewModel>();
                if (shell.Current is null)
                {
                    var login = SafeResolve<LoginViewModel>();
                    shell.Current = login;
                }

                main.DataContext = shell;

                _ = TryResolve<DashboardViewModel>();
                _ = TryResolve<FrontDeskViewModel>();
                _ = TryResolve<HousekeepingViewModel>();
                _ = TryResolve<ProfileViewModel>();
                _ = TryResolve<SettingsViewModel>();
            }
            catch (Exception ex)
            {
                main.Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = "Startup error:\n\n" + ex,
                        TextWrapping = TextWrapping.Wrap
                    },
                    Margin = new Thickness(20)
                };
            }

            base.OnFrameworkInitializationCompleted();
            return;
        }

        // Single-view lifetime (fallback; some environments use this)
        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            // Build a simple host view using your MainWindow content & VM
            var hostView = new ContentControl();

            try
            {
                if (_xamlLoadError is not null)
                    throw new InvalidOperationException("App XAML failed to load.", _xamlLoadError);

                var shell = SafeResolve<MainViewModel>();
                if (shell.Current is null)
                {
                    var login = SafeResolve<LoginViewModel>();
                    shell.Current = login;
                }

                // Reuse MainWindow’s visual tree by instantiating it and extracting the Content
                var tmpWindow = new MainWindow();
                hostView.Content = tmpWindow.Content;
                hostView.DataContext = shell;
            }
            catch (Exception ex)
            {
                hostView.Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = "Startup error:\n\n" + ex,
                        TextWrapping = TextWrapping.Wrap
                    },
                    Margin = new Thickness(20)
                };
            }

            single.MainView = hostView;

            base.OnFrameworkInitializationCompleted();
            return;
        }

        // If neither lifetime matched, at least finish cleanly
        base.OnFrameworkInitializationCompleted();
    }

    // --- Helpers -------------------------------------------------------------

    private T SafeResolve<T>() where T : class
        => TryResolve<T>() ?? throw new InvalidOperationException(
            $"Unable to resolve {typeof(T).Name}. Make sure it is registered in DI.");

    private T? TryResolve<T>() where T : class
    {
        var svc = _services.GetService<T>();
        if (svc is not null) return svc;

        try { return ActivatorUtilities.CreateInstance<T>(_services); }
        catch { /* fall through */ }

        try { return (T?)Activator.CreateInstance(typeof(T), nonPublic: true); }
        catch { return null; }
    }
}
