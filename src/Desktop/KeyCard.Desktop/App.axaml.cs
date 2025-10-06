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

        // 1) Always create + show a window *first*, so the lifetime stays alive.
        var main = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Title = "KeyCard.NET — Staff Console"
        };
        desktop.MainWindow = main;
        main.Show();

        // 2) Now try to wire DI + VM. If anything fails, keep the window and display the error.
        try
        {
            if (_xamlLoadError is not null)
                throw new InvalidOperationException("App XAML failed to load.", _xamlLoadError);

            // Resolve the shell (MainViewModel) from DI
            var shell = _services.GetRequiredService<MainViewModel>();

            // Ensure we start at Login if nothing has set a page yet
            if (shell.Current is null)
            {
                var login = _services.GetRequiredService<LoginViewModel>();
                shell.Current = login;
            }

            // Bind the shell VM to the already-shown window
            main.DataContext = shell;
        }
        catch (Exception ex)
        {
            // Replace window content with a readable error instead of exiting silently
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
}
