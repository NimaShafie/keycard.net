// Program.cs (Desktop)
using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;                    // for ShutdownMode
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;

using KeyCard.Desktop.Configuration;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Views;                // MainWindow (kept for type refs)
using KeyCard.Desktop.ViewModels;
using KeyCard.Desktop.Infrastructure.Api;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop
{
    public static class Program
    {
        private static readonly string _logPath =
            Path.Combine(Path.GetTempPath(), "keycard_desktop.log");

        [STAThread]
        public static async Task Main(string[] args)
        {
            WriteBreadcrumb("Main() entered");

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                WriteBreadcrumb("UnhandledException: " + e.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                WriteBreadcrumb("UnobservedTaskException: " + e.Exception);
                e.SetObserved();
            };

            var builder = Host.CreateDefaultBuilder(args)
                // keep validation relaxed so UI can show even if some services are swapped later
                .UseDefaultServiceProvider(o => { o.ValidateOnBuild = false; o.ValidateScopes = false; });

            // --- Respect launch profile / shell env first, then fall back ---
            var envFromProfile = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(envFromProfile))
                envFromProfile = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrWhiteSpace(envFromProfile))
            {
#if DEBUG
                builder = builder.UseEnvironment(Environments.Development);
                WriteBreadcrumb("Environment defaulted to Development (no DOTNET_ENVIRONMENT/ASPNETCORE_ENVIRONMENT).");
#else
                builder = builder.UseEnvironment(Environments.Production);
                WriteBreadcrumb("Environment defaulted to Production (no DOTNET_ENVIRONMENT/ASPNETCORE_ENVIRONMENT).");
#endif
            }
            else
            {
                builder = builder.UseEnvironment(envFromProfile);
                WriteBreadcrumb($"Environment from profile/shell: {envFromProfile}");
            }
            // ----------------------------------------------------------------

            var host = builder
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var env = ctx.HostingEnvironment;
                    cfg.Sources.Clear();
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                       .AddEnvironmentVariables();
                    WriteBreadcrumb($"Config loaded for {env.EnvironmentName}");
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddLogging(b => b.AddConsole());

                    services.Configure<KeyCardOptions>(ctx.Configuration.GetSection("KeyCard"));
                    services.Configure<ApiOptions>(ctx.Configuration.GetSection("Api"));
                    services.Configure<SignalROptions>(ctx.Configuration.GetSection("SignalR"));

                    services.AddSingleton<IAppEnvironment, AppEnvironment>();

                    // Navigation service needed by VMs
                    services.AddSingleton<INavigationService, NavigationService>();

                    // Back-compat shim
                    services.AddSingleton<IAppConfig>(sp =>
                    {
                        var env = sp.GetRequiredService<IAppEnvironment>();
                        return new AppConfig(env);
                    });

                    // Decide Mock vs Live based on the centralized env
                    using (var temp = services.BuildServiceProvider())
                    {
                        var appEnv = temp.GetRequiredService<IAppEnvironment>();
                        WriteBreadcrumb($"IsMock={appEnv.IsMock}, ApiBaseUrl={appEnv.ApiBaseUrl}");

                        if (appEnv.IsMock)
                        {
                            services.AddSingleton<IAuthService, MockAuthService>();
                            services.AddSingleton<IBookingService, MockBookingService>();
                            services.AddSingleton<IHousekeepingService, MockHousekeepingService>();
                        }
                        else
                        {
                            // low-level API client registrations
                            services.AddKeyCardApi(ctx.Configuration, appEnv);

                            // when ready to go fully Live with your VMs, register adapters:
                            // services.AddSingleton<IAuthService, LiveAuthServiceAdapter>();
                            // services.AddSingleton<IBookingService, LiveBookingServiceAdapter>();
                            // services.AddSingleton<IHousekeepingService, LiveHousekeepingServiceAdapter>();
                        }
                    }

                    // SignalR selector
                    services.AddSingleton<ISignalRService>(sp =>
                    {
                        var env = sp.GetRequiredService<IAppEnvironment>();
                        if (env.IsMock) return new NoOpSignalRService();
                        var url = env.BookingsHubUrl;
                        return ActivatorUtilities.CreateInstance<SignalRService>(sp, url);
                    });

                    // VMs
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<FrontDeskViewModel>();
                    services.AddTransient<HousekeepingViewModel>();
                    services.AddTransient<ProfileViewModel>();
                    services.AddTransient<SettingsViewModel>();

                    // Do NOT register MainWindow in DI; it's created in App.axaml.cs
                    // services.AddTransient<MainWindow>();

                    // App
                    services.AddSingleton<App>();
                })
                .Build();

            try
            {
                await host.StartAsync();
                WriteBreadcrumb("Host started");
            }
            catch (Exception ex)
            {
                WriteBreadcrumb("Host failed to start: " + ex);
                throw;
            }

            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
            var envSvc = host.Services.GetRequiredService<IAppEnvironment>();
            var mode = envSvc.IsMock ? "Mock" : "Live";
            logger.LogInformation("KeyCard.Desktop starting — Mode={Mode} IsMock={IsMock} ApiBaseUrl={ApiBaseUrl}",
                mode, envSvc.IsMock, envSvc.ApiBaseUrl);
            WriteBreadcrumb($"Starting Avalonia — Mode={mode}, IsMock={envSvc.IsMock}, ApiBaseUrl={envSvc.ApiBaseUrl}");

            Environment.SetEnvironmentVariable("AVALONIA_PLATFORM", "Win32");
            WriteBreadcrumb("AVALONIA_PLATFORM=Win32");

            try
            {
                // Standard classic desktop startup (App.axaml.cs shows the window)
                BuildAvaloniaApp(host).StartWithClassicDesktopLifetime(args);
                WriteBreadcrumb("Avalonia exited normally");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Avalonia failed to start.");
                WriteBreadcrumb("Avalonia startup error: " + ex);
                throw;
            }
            finally
            {
                try
                {
                    await host.StopAsync();
                    WriteBreadcrumb("Host stopped");
                }
                catch (Exception ex)
                {
                    WriteBreadcrumb("Host stop error: " + ex);
                }
                host.Dispose();
                WriteBreadcrumb("Host disposed");
            }
        }

        public static AppBuilder BuildAvaloniaApp(IHost host)
            => AppBuilder.Configure(() => host.Services.GetRequiredService<App>())
                         .UsePlatformDetect()
                         .LogToTrace()
                         .UseReactiveUI();

        private static void WriteBreadcrumb(string line)
        {
            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {line}{Environment.NewLine}");
            }
            catch { /* ignore */ }
        }
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
