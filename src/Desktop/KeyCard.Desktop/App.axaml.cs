// src/Desktop/KeyCard.Desktop/App.axaml.cs
using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

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
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        // 1) Always create + show a window first so the lifetime stays alive
        var main = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Title = "KeyCard.NET — Staff Console"
        };
        desktop.MainWindow = main;
        main.Show();

        // 2) Now wire DI + VM. If anything fails, keep the window and display the error.
        try
        {
            if (_xamlLoadError is not null)
                throw new InvalidOperationException("App XAML failed to load.", _xamlLoadError);

            // Resolve the shell (MainViewModel) from DI; fall back to ActivatorUtilities if needed
            var shell = SafeResolve<MainViewModel>();

            // Ensure we start at Login if nothing has set a page yet
            if (shell.Current is null)
            {
                var login = SafeResolve<LoginViewModel>();
                shell.Current = login;
            }

            // Bind the shell VM to the already-shown window
            main.DataContext = shell;

            // Optional: warm up commonly navigated VMs so missing registrations show at startup
            _ = TryResolve<DashboardViewModel>();
            _ = TryResolve<FrontDeskViewModel>();
            _ = TryResolve<HousekeepingViewModel>();
            _ = TryResolve<ProfileViewModel>();
            _ = TryResolve<SettingsViewModel>();
        }
        catch (Exception ex)
        {
            // Replace window content with a readable error instead of exiting
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
    }

    // --- Helpers -------------------------------------------------------------

    private T SafeResolve<T>() where T : class
        => TryResolve<T>() ?? throw new InvalidOperationException(
            $"Unable to resolve {typeof(T).Name}. Make sure it is registered in DI.");

    private T? TryResolve<T>() where T : class
    {
        // 1) DI
        var svc = _services.GetService<T>();
        if (svc is not null) return svc;

        // 2) ActivatorUtilities (injects any known deps from the container)
        try { return ActivatorUtilities.CreateInstance<T>(_services); }
        catch { /* fall through */ }

        // 3) public or non-public parameterless ctor
        try { return (T?)Activator.CreateInstance(typeof(T), nonPublic: true); }
        catch { return null; }
    }
}
