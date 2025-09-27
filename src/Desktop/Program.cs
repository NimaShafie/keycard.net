using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using KeyCard.Desktop.Infrastructure;

namespace KeyCard.Desktop;

internal static class Program
{
    public static IHost AppHost { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            File.AppendAllText("startup.log", $"[Unhandled] {e.ExceptionObject}\n");

        try
        {
            AppHost = Bootstrap.BuildHost(args);
            AppHost.Start();
            Console.WriteLine("[Program] Host started. Launching Avalonia…");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Console.WriteLine("[Program] Avalonia lifetime ended. Disposing host…");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Program] Fatal: " + ex);
            File.AppendAllText("startup.log", $"[Fatal] {ex}\n");
            throw;
        }
        finally
        {
            AppHost.Dispose();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure(() => new App(AppHost.Services))
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
