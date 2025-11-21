// Program.cs
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;

using KeyCard.Desktop.Configuration;
using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Infrastructure.Api;
using KeyCard.Desktop.Modules.Folio;
using KeyCard.Desktop.Modules.Folio.ViewModels;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Services.Api;
using KeyCard.Desktop.Services.Live;
using KeyCard.Desktop.Services.Mock;
using KeyCard.Desktop.Startup;
using KeyCard.Desktop.ViewModels;
using KeyCard.Desktop.Views;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using IAppEnvironment = KeyCard.Desktop.Services.IAppEnvironment;

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
                .UseDefaultServiceProvider(o => { o.ValidateOnBuild = false; o.ValidateScopes = false; });

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

            var host = builder
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var env = ctx.HostingEnvironment;
                    var baseDir = AppContext.BaseDirectory;
                    var configDir = Path.Combine(baseDir, "Configuration");
                    Directory.CreateDirectory(configDir);
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
                    services.AddSingleton<IToolbarService, ToolbarService>();

                    services.Configure<KeyCardOptions>(ctx.Configuration.GetSection("KeyCard"));
                    services.Configure<ApiOptions>(ctx.Configuration.GetSection("Api"));
                    services.Configure<SignalROptions>(ctx.Configuration.GetSection("SignalR"));
                    services.Configure<RoutesOptions>(ctx.Configuration.GetSection("Api:Routes"));

                    services.AddSingleton<IAppEnvironment, AppEnvironment>();
                    services.AddSingleton<IBookingStateService, BookingStateService>();
                    services.AddSingleton<INavigationService, NavigationService>();

                    services.AddSingleton<IAppConfig>(sp =>
                    {
                        var env = sp.GetRequiredService<IAppEnvironment>();
                        return new AppConfig(env);
                    });

                    using (var temp = services.BuildServiceProvider())
                    {
                        var appEnv = temp.GetRequiredService<IAppEnvironment>();
                        services.AddFolioModule(appEnv);

                        WriteBreadcrumb($"IsMock={appEnv.IsMock}, ApiBaseUrl={appEnv.ApiBaseUrl}");

                        if (appEnv.IsMock)
                        {
                            services.AddSingleton<IAuthService, Services.Mock.AuthService>();
                            services.AddSingleton<IBookingService, Services.Mock.BookingService>();
                            services.AddSingleton<IHousekeepingService, Services.Mock.HousekeepingService>();
                        }
                        else
                        {
                            services.AddKeyCardApi(ctx.Configuration, appEnv);

                            // ✅ CRITICAL FIX: Add SSL bypass for localhost development
                            services.AddHttpClient("KeyCardApi", (sp, http) =>
                            {
                                var env = sp.GetRequiredService<IAppEnvironment>();
                                http.BaseAddress = new Uri(env.ApiBaseUrl, UriKind.Absolute);
                                http.Timeout = TimeSpan.FromSeconds(30);
                            })
                            .ConfigurePrimaryHttpMessageHandler(() =>
                            {
                                return new HttpClientHandler
                                {
                                    // CRITICAL: Accept self-signed certificates for localhost development
                                    // Remove this in production!
                                    ServerCertificateCustomValidationCallback =
                                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                                };
                            });

                            // Also register the "Api" client name for AuthServiceAdapter
                            services.AddHttpClient("Api", (sp, http) =>
                            {
                                var env = sp.GetRequiredService<IAppEnvironment>();
                                http.BaseAddress = new Uri(env.ApiBaseUrl, UriKind.Absolute);
                                http.Timeout = TimeSpan.FromSeconds(30);
                                http.DefaultRequestHeaders.Add("Accept", "application/json");
                            })
                            .ConfigurePrimaryHttpMessageHandler(() =>
                            {
                                return new HttpClientHandler
                                {
                                    ServerCertificateCustomValidationCallback =
                                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                                };
                            });

                            services.AddTransient(sp =>
                            {
                                var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("KeyCardApi");
                                return new KeyCard.Desktop.Generated.KeyCardApiClient(http);
                            });

                            services.AddSingleton<IBookingService, KeyCard.Desktop.Services.Api.BookingService>();

                            WriteBreadcrumb("LIVE mode: Registering AuthServiceAdapter and HousekeepingServiceAdapter");

                            // Remove the duplicate mock registrations and use only the live adapters
                            services.AddSingleton<IAuthService, AuthServiceAdapter>();
                            services.AddSingleton<IHousekeepingService, HousekeepingServiceAdapter>();
                            services.AddSingleton<IBookingService, BookingServiceAdapter>();
                        }
                        services.AddRoomsService(appEnv);
                    }

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
                            return ActivatorUtilities.CreateInstance<KeyCard.Desktop.Services.Live.SignalRService>(sp, url);
                        }
                        catch (Exception ex)
                        {
                            WriteBreadcrumb("SignalR creation failed, falling back to NoOp: " + ex.Message);
                            return new NoOpSignalRService();
                        }
                    });

                    // ✅ CRITICAL FIX: ViewModels MUST be Singleton to preserve state across navigation
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<LoginViewModel>();
                    services.AddSingleton<RegistrationViewModel>();  // ✅ Added missing ViewModel
                    services.AddSingleton<DashboardViewModel>();
                    services.AddSingleton<FrontDeskViewModel>();
                    services.AddSingleton<HousekeepingViewModel>();
                    services.AddSingleton<FolioViewModel>();
                    services.AddSingleton<ProfileViewModel>();
                    services.AddSingleton<SettingsViewModel>();

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

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!envSvc.IsMock && !string.IsNullOrWhiteSpace(envSvc.ApiBaseUrl))
                    {
                        using var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback =
                                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        };
                        using var http = new HttpClient(handler) { BaseAddress = new Uri(envSvc.ApiBaseUrl) };
                        http.Timeout = TimeSpan.FromSeconds(5);
                        var resp = await http.GetAsync("/api/v1/Health");
                        var txt = await resp.Content.ReadAsStringAsync();
                        WriteBreadcrumb($"Live health probe: {(int)resp.StatusCode} {resp.ReasonPhrase} Body='{txt}'");
                    }
                }
                catch (Exception ex)
                {
                    WriteBreadcrumb("Live health probe failed: " + ex.Message);
                }
            });

            Environment.SetEnvironmentVariable("AVALONIA_PLATFORM", "Win32");
            WriteBreadcrumb("AVALONIA_PLATFORM=Win32");

            try
            {
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
