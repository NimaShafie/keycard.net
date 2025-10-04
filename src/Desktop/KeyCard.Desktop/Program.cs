using System;
using System.IO;

using Avalonia;
using Avalonia.ReactiveUI;

using KeyCard.Desktop.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
            var opts = AppHost.Services.GetRequiredService<IOptions<KeyCardOptions>>().Value;

            // Back-compat: allow legacy single value (ApiBaseUrl / API_BASE_URL) to win if supplied
            var legacyApiBase =
                cfg["ApiBaseUrl"] ?? Environment.GetEnvironmentVariable("API_BASE_URL");

            var effectiveApiBase =
                string.Equals(opts.Mode, "Live", StringComparison.OrdinalIgnoreCase)
                    ? (legacyApiBase ?? opts.Api.HttpsBaseUrl)
                    : "(mock)";

            Console.WriteLine(
                $"[Program] Host started. Mode={opts.Mode}; EffectiveApiBase={effectiveApiBase}; " +
                $"HttpsBase={opts.Api.HttpsBaseUrl}; HttpBase={opts.Api.HttpBaseUrl}. Launching Avalonia…");

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

                // Start clean, then add only what we want (keeps your original behavior)
                cfg.Sources.Clear();
                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                   // Extra override when running against docker-compose (kept from your code)
                   .AddJsonFile($"appsettings.{env.EnvironmentName}.Container.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables() // allows API_BASE_URL and KeyCard__Mode, etc.
                   .AddCommandLine(args);     // allow: KeyCard:Mode=Live (new, for easy switching)
            })
            .ConfigureServices((context, services) =>
            {
                // Bind options once so everyone (including AddDesktopServices) can consume them
                services.Configure<KeyCardOptions>(context.Configuration.GetSection("KeyCard"));

                // Registers HttpClient + NSwag API client (live or mock), SignalR, your Services/* and ViewModels/*
                services.AddDesktopServices(context.Configuration);
            })
            .Build();

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure(() => new App(AppHost.Services))
                  .UsePlatformDetect()
                  .LogToTrace()
                  .UseReactiveUI();
}
