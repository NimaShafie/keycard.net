using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.ReactiveUI;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var env = ctx.HostingEnvironment;

                    // Keep your environment-specific config loading behavior
                    cfg.Sources.Clear();
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                       .AddEnvironmentVariables();
                })
                .ConfigureServices((ctx, services) =>
                {
                    // Logging (console)
                    services.AddLogging(b => b.AddConsole());

                    // Strongly-typed app config (kept)
                    services.AddSingleton<IAppConfig>(sp =>
                    {
                        var cfg = sp.GetRequiredService<IConfiguration>();
                        return new AppConfig(cfg);
                    });

                    // Core services (kept)
                    services.AddSingleton<Services.INavigationService, Services.NavigationService>();
                    services.AddSingleton<Services.IAuthService, Services.MockAuthService>(); // swap later when wiring real auth

                    // App services used by VMs (kept/added earlier)
                    services.AddSingleton<Services.IBookingService, Services.MockBookingService>();
                    services.AddSingleton<Services.IHousekeepingService, Services.MockHousekeepingService>();

                    // ---- SignalR service registration (FIX) ----
                    var isMock =
                        string.Equals(ctx.Configuration["KeyCard:Mode"], "Mock", StringComparison.OrdinalIgnoreCase) ||
                        (bool.TryParse(ctx.Configuration["UseMocks"], out var useMocks) && useMocks);

                    if (isMock)
                    {
                        // No-op in Mock mode
                        services.AddSingleton<Services.ISignalRService, NoOpSignalRService>();
                    }
                    else
                    {
                        // Provide the required string ctor arg (hub URL) from configuration.
                        services.AddSingleton<Services.ISignalRService>(sp =>
                        {
                            var url = ResolveBookingsHubUrl(sp);
                            // ActivatorUtilities will satisfy other ctor deps automatically and pass our string.
                            return ActivatorUtilities.CreateInstance<Services.SignalRService>(sp, url);
                        });
                    }

                    // ViewModels (kept + ensure all navigated VMs are registered)
                    services.AddSingleton<ViewModels.LoginViewModel>();
                    services.AddSingleton<ViewModels.DashboardViewModel>();
                    services.AddSingleton<ViewModels.FrontDeskViewModel>();
                    services.AddSingleton<ViewModels.HousekeepingViewModel>();
                    services.AddSingleton<ViewModels.MainViewModel>(); // required by App startup

                    // App from DI (kept)
                    services.AddSingleton<App>();
                })
                .Build();

            // Start Avalonia using the App instance from DI (kept)
            BuildAvaloniaApp(host).StartWithClassicDesktopLifetime(args);

            await Task.CompletedTask;
        }

        public static AppBuilder BuildAvaloniaApp(IHost host)
            => AppBuilder.Configure(() => host.Services.GetRequiredService<App>())
                         .UsePlatformDetect()
                         .LogToTrace()
                         .UseReactiveUI();

        /// <summary>
        /// Picks the bookings hub URL from config. You can override with:
        /// "SignalR:BookingsHubUrl": "https://your-api/hubs/bookings"
        /// Otherwise it falls back to Api:BaseUrl + "/hubs/bookings".
        /// </summary>
        private static string ResolveBookingsHubUrl(IServiceProvider sp)
        {
            var cfg = sp.GetRequiredService<IConfiguration>();

            var explicitUrl = cfg["SignalR:BookingsHubUrl"];
            if (!string.IsNullOrWhiteSpace(explicitUrl))
                return explicitUrl;

            var baseUrl = cfg["Api:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = "http://localhost:5001";

            return CombineUri(baseUrl, "hubs/bookings");
        }

        private static string CombineUri(string baseUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return baseUrl;
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            return baseUrl + path.TrimStart('/');
        }
    }

    public interface IAppConfig
    {
        string Mode { get; }
        bool UseMocks { get; }
        string ApiBaseUrl { get; }
    }

    public class AppConfig : IAppConfig
    {
        public string Mode { get; }
        public bool UseMocks { get; }
        public string ApiBaseUrl { get; }

        public AppConfig(IConfiguration cfg)
        {
            Mode = cfg["KeyCard:Mode"] ?? "Live";
            UseMocks = bool.TryParse(cfg["UseMocks"], out var b) && b;
            ApiBaseUrl = cfg["Api:BaseUrl"] ?? "";
        }
    }

    /// <summary>
    /// No-op SignalR for Mock/dev mode: satisfies ISignalRService so MainViewModel can start,
    /// but avoids making any real network calls.
    /// </summary>
    internal sealed class NoOpSignalRService : Services.ISignalRService
    {
        public HubConnection BookingsHub { get; }

        public NoOpSignalRService()
        {
            // Harmless placeholder connection; we never StartAsync on it.
            BookingsHub = new HubConnectionBuilder()
                .WithUrl("http://localhost/dev-null")
                .Build();
        }

        public Task StartAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
    }
}
