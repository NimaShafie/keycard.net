using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;

using KeyCard.Desktop.Services;
using KeyCard.Desktop.ViewModels;
using KeyCard.Desktop.Views;

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    public App(IServiceProvider services) => _services = services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Console.WriteLine("[App] OnFrameworkInitializationCompleted()");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                // Shell + starting page
                var shell = _services.GetRequiredService<MainViewModel>();
                if (shell.Current is null)
                {
                    Console.WriteLine("[App] Setting initial VM -> LoginViewModel");
                    shell.Current = _services.GetRequiredService<LoginViewModel>();
                }

                // Ensure nav service is constructed (no-op but helps later navigation)
                //_ = _services.GetRequiredService<INavigationService>();

                // Create the window yourself and bind the shell VM
                Console.WriteLine("[App] Creating MainWindow(shell) …");
                var main = new MainWindow(shell)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                // Optional belt-and-suspenders: VM->View mapping in code
                // main.DataTemplates.Add(new FuncDataTemplate<LoginViewModel>((_,__) => new LoginView(), true));

                desktop.MainWindow = main;
                Console.WriteLine("[App] Showing MainWindow …");
                main.Show();
                Console.WriteLine("[App] MainWindow.Show() done");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[App] ERROR while creating window: " + ex);
                // Force at least *some* window so the loop doesn't exit silently
                var fallback = new Window
                {
                    Content = new TextBlock { Text = "Startup error: " + ex.Message, Margin = new Avalonia.Thickness(20) },
                    Width = 800,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                desktop.MainWindow = fallback;
                fallback.Show();
            }
        }
        else
        {
            Console.WriteLine("[App] Non-desktop lifetime (unexpected on Windows).");
        }

        base.OnFrameworkInitializationCompleted();
    }

}
