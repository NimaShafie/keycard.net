// Program.cs (Desktop)
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;

using KeyCard.Desktop.Configuration;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Views;
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

                    // centralized config folder alongside binaries
                    var baseDir = AppContext.BaseDirectory;
                    var configDir = Path.Combine(baseDir, "Configuration");
                    Directory.CreateDirectory(configDir); // harmless if exists
                    WriteBreadcrumb($"Config base directory: {configDir}");

                    var launchProfile = Environment.GetEnvironmentVariable("DOTNET_LAUNCH_PROFILE") ?? string.Empty;
                    var launchProfileTag = launchProfile.Contains("mock", StringComparison.OrdinalIgnoreCase) ? "Mock"
                                           : launchProfile.Contains("live", StringComparison.OrdinalIgnoreCase) ? "Live"
                                           : null;

                    var explicitMode = Environment.GetEnvironmentVariable("KEYCARD_MODE");
                    if (!string.IsNullOrWhiteSpace(explicitMode))
                    {
                        if (explicitMode.Equals("Live", StringComparison.OrdinalIgnoreCase)) launchProfileTag = "Live";
                        else if (explicitMode.Equals("Mock", StringComparison.OrdinalIgnoreCase)) launchProfileTag = "Mock";
                    }

                    cfg.Sources.Clear();
                    cfg.SetBasePath(configDir);

                    AddJsonIfExists(cfg, "appsettings.json", optional: true, reloadOnChange: true);
                    AddJsonIfExists(cfg, $"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                    if (!string.IsNullOrWhiteSpace(launchProfileTag))
                    {
                        AddJsonIfExists(cfg, $"appsettings.{launchProfileTag}.json", optional: true, reloadOnChange: true);
                        WriteBreadcrumb($"Applied profile override: {launchProfileTag} (from DOTNET_LAUNCH_PROFILE='{launchProfile}' / KEYCARD_MODE='{explicitMode}')");
                    }
                    else
                    {
                        WriteBreadcrumb("No profile override detected (DOTNET_LAUNCH_PROFILE/KEYCARD_MODE).");
                    }

                    AddJsonIfExists(cfg, "appsettings.Local.json", optional: true, reloadOnChange: true);
                    cfg.AddEnvironmentVariables();

                    WriteBreadcrumb($"Config loaded for {env.EnvironmentName} — files: " +
                        string.Join(", ",
                            cfg.Sources.OfType<Microsoft.Extensions.Configuration.Json.JsonConfigurationSource>()
                            .Select(s => Path.GetFileName(s.Path))));
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddLogging(b => b.AddConsole());

                    services.Configure<KeyCardOptions>(ctx.Configuration.GetSection("KeyCard"));
                    services.Configure<ApiOptions>(ctx.Configuration.GetSection("Api"));
                    services.Configure<SignalROptions>(ctx.Configuration.GetSection("SignalR"));
                    // Bind API routes for LIVE services (e.g., BookingService)
                    services.Configure<ApiRoutesOptions>(ctx.Configuration.GetSection("Api:Routes"));

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

                            // LIVE: register services so app doesn't crash even if backend is down.
                            // Booking goes to the real live service (already resilient to HTTP failures).
                            services.AddSingleton<IBookingService, BookingService>();

                            // Auth + Housekeeping: TEMPORARY safe fallback to mocks in LIVE so the app boots.
                            // We also log a breadcrumb so it’s visible this is a fallback.
                            WriteBreadcrumb("LIVE mode: IAuthService & IHousekeepingService temporarily bound to mock implementations (backend not required to boot).");
                            services.AddSingleton<IAuthService, MockAuthService>();
                            services.AddSingleton<IHousekeepingService, MockHousekeepingService>();

                            // When you’re ready, replace the two above with real adapters:
                            services.AddSingleton<IAuthService, LiveAuthServiceAdapter>();
                            services.AddSingleton<IHousekeepingService, LiveHousekeepingServiceAdapter>();
                        }
                    }

                    // SignalR selector — return NoOp if URL absent to avoid crashes in LIVE with no backend.
                    services.AddSingleton<ISignalRService>(sp =>
                    {
                        var env = sp.GetRequiredService<IAppEnvironment>();
                        var url = env.BookingsHubUrl;

                        if (env.IsMock || string.IsNullOrWhiteSpace(url))
                        {
                            WriteBreadcrumb("SignalR: Using NoOpSignalRService (mock mode or empty hub URL).");
                            return new NoOpSignalRService();
                        }

                        try
                        {
                            return ActivatorUtilities.CreateInstance<SignalRService>(sp, url);
                        }
                        catch (Exception ex)
                        {
                            WriteBreadcrumb("SignalR creation failed, falling back to NoOp: " + ex.Message);
                            return new NoOpSignalRService();
                        }
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

        private static void AddJsonIfExists(IConfigurationBuilder cfg, string file, bool optional, bool reloadOnChange)
        {
            try
            {
                cfg.AddJsonFile(file, optional: optional, reloadOnChange: reloadOnChange);
            }
            catch (Exception ex)
            {
                WriteBreadcrumb($"AddJsonIfExists('{file}') error: {ex.Message}");
            }
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
