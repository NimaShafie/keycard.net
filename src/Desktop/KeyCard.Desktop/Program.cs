// Program.cs
using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.ReactiveUI;

using KeyCard.Desktop.Configuration;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.ViewModels; // <-- Added for VM registrations

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeyCard.Desktop
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

#if DEBUG
            // Load appsettings.Development.json automatically during Debug runs
            builder = builder.UseEnvironment(Environments.Development);
#endif

            var host = builder
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var env = ctx.HostingEnvironment;

                    cfg.Sources.Clear();
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                       .AddEnvironmentVariables();
                })
                .ConfigureServices((ctx, services) =>
                {
                    // Logging
                    services.AddLogging(b => b.AddConsole());

                    // Bind options (single source of truth for settings)
                    services.Configure<KeyCardOptions>(ctx.Configuration.GetSection("KeyCard"));
                    services.Configure<ApiOptions>(ctx.Configuration.GetSection("Api"));
                    services.Configure<SignalROptions>(ctx.Configuration.GetSection("SignalR"));

                    // Central environment/config service
                    services.AddSingleton<IAppEnvironment, AppEnvironment>();

                    // Back-compat shim for any code still using IAppConfig
                    services.AddSingleton<IAppConfig>(sp =>
                    {
                        var env = sp.GetRequiredService<IAppEnvironment>();
                        return new AppConfig(env);
                    });

                    // Core services
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<IAuthService, MockAuthService>(); // swap later for real auth

                    // App services used by VMs
                    services.AddSingleton<IBookingService, MockBookingService>();
                    services.AddSingleton<IHousekeepingService, MockHousekeepingService>();

                    // SignalR service: choose No-Op in Mock, real in Live — using the centralized env
                    services.AddSingleton<ISignalRService>(sp =>
                    {
                        var env = sp.GetRequiredService<IAppEnvironment>();
                        if (env.IsMock) return new NoOpSignalRService();

                        var url = env.BookingsHubUrl;
                        return ActivatorUtilities.CreateInstance<SignalRService>(sp, url);
                    });

                    // =========================
                    // ViewModels (DI registrations)
                    // =========================

                    // Shell
                    services.AddSingleton<MainViewModel>();

                    // Navigated pages: use Transient so each navigation can get a fresh instance if needed
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<FrontDeskViewModel>();
                    services.AddTransient<HousekeepingViewModel>();
                    services.AddTransient<ProfileViewModel>();
                    services.AddTransient<SettingsViewModel>();

                    // App instance
                    services.AddSingleton<App>();
                })
                .Build();

            BuildAvaloniaApp(host).StartWithClassicDesktopLifetime(args);
            await Task.CompletedTask;
        }

        public static AppBuilder BuildAvaloniaApp(IHost host)
            => AppBuilder.Configure(() => host.Services.GetRequiredService<App>())
                         .UsePlatformDetect()
                         .LogToTrace()
                         .UseReactiveUI();
    }

    // ---- Back-compat shim so old code can still inject IAppConfig if needed ----
    public interface IAppConfig
    {
        string Mode { get; }
        bool UseMocks { get; }
        string ApiBaseUrl { get; }
    }

    public sealed class AppConfig : IAppConfig
    {
        public string Mode { get; }
        public bool UseMocks { get; }
        public string ApiBaseUrl { get; }

        public AppConfig(IAppEnvironment env)
        {
            Mode = env.IsMock ? "Mock" : "Live";
            UseMocks = env.IsMock;
            ApiBaseUrl = env.ApiBaseUrl;
        }
    }

    /// <summary>No-op SignalR for Mock/dev mode.</summary>
    internal sealed class NoOpSignalRService : ISignalRService
    {
        public HubConnection BookingsHub { get; }

        public NoOpSignalRService()
        {
            BookingsHub = new HubConnectionBuilder()
                .WithUrl("http://localhost/dev-null")
                .Build();
        }

        public Task StartAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
    }
}
