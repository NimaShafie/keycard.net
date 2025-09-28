using System;
using System.IO;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            AppHost = BuildHost(args);
            AppHost.Start();

            var cfg = AppHost.Services.GetRequiredService<IConfiguration>();
            var apiBase = cfg["ApiBaseUrl"] ?? Environment.GetEnvironmentVariable("API_BASE_URL") ?? "(not set)";
            Console.WriteLine($"[Program] Host started. ApiBaseUrl = {apiBase}. Launching Avalonia…");

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
            try { AppHost?.Dispose(); } catch { /* ignore */ }
        }
    }

    private static IHost BuildHost(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, cfg) =>
            {
                var env = context.HostingEnvironment;

                cfg.Sources.Clear(); // start clean, then add what we want
                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                   // extra override when running against docker-compose
                   .AddJsonFile($"appsettings.{env.EnvironmentName}.Container.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables(); // allows API_BASE_URL, etc.
            })
            .ConfigureServices((context, services) =>
            {
                // Registers HttpClient + NSwag API client, SignalR service, your Services/* and ViewModels/*
                services.AddDesktopServices(context.Configuration);
            })
            .Build();

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure(() => new App(AppHost.Services))
                  .UsePlatformDetect()
                  .LogToTrace()
                  .UseReactiveUI();
}
