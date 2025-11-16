// Infrastructure/Bootstrap.cs
using System;
using System.Net.Http;
using System.Net.Http.Headers;

using KeyCard.Desktop.Generated;
using KeyCard.Desktop.Modules.Folio.Services;
using KeyCard.Desktop.Modules.Folio.ViewModels;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.ViewModels;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.Extensions.Http;

namespace KeyCard.Desktop.Infrastructure
{
    // ✅ GLOBAL static storage for ViewModels - survives app restarts
    internal static class ViewModelCache
    {
        public static HousekeepingViewModel? HousekeepingInstance { get; set; }
        public static DashboardViewModel? DashboardInstance { get; set; }
        public static FrontDeskViewModel? FrontDeskInstance { get; set; }
    }

    public static class Bootstrap
    {
        public static IServiceCollection AddDesktopServices(
            this IServiceCollection services,
            IConfiguration cfg)
        {
            var apiBase = cfg["ApiBaseUrl"]
                ?? Environment.GetEnvironmentVariable("API_BASE_URL")
                ?? "http://localhost:8080";

            // Polly retry policy
            static IAsyncPolicy<HttpResponseMessage> RetryPolicy() =>
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(r => (int)r.StatusCode == 429)
                    .WaitAndRetryAsync(new[]
                    {
                        TimeSpan.FromMilliseconds(200),
                        TimeSpan.FromMilliseconds(500),
                        TimeSpan.FromSeconds(1),
                    });

            // HTTP client for API
            services.AddHttpClient("Api", c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler(() => new RetryDelegatingHandler(RetryPolicy()));

            // Typed API client
            services.AddSingleton(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new KeyCardApiClient(factory.CreateClient("Api"));
            });

            // SignalR
            services.AddSingleton<ISignalRService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                return env.IsMock
                    ? new NoOpSignalRService()
                    : new SignalRService(env.BookingsHubUrl);
            });

            // Business services - Booking
            services.AddSingleton<IBookingService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                if (env.IsMock)
                    return new Services.Mock.BookingService();
                else
                    return ActivatorUtilities.CreateInstance<Services.BookingService>(sp);
            });

            // BookingStateService
            services.AddSingleton<IBookingStateService, BookingStateService>();

            // Housekeeping
            services.AddSingleton<IHousekeepingService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                if (env.IsMock)
                    return new Services.Mock.HousekeepingService();
                else
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    return new Services.Api.HousekeepingService(factory.CreateClient("Api"));
                }
            });

            // Auth
            services.AddSingleton<IAuthService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                return env.IsMock
                    ? new Services.Mock.AuthService()
                    : new Services.Api.AuthService();
            });

            // Folio
            services.AddSingleton<IFolioService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                if (env.IsMock)
                    return new MockFolioService();
                else
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    return new LiveFolioService(factory.CreateClient("Api"));
                }
            });

            // Rooms
            services.AddSingleton<IRoomsService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                if (env.IsMock)
                    return new Services.Mock.RoomsService();
                else
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    return new Services.Live.RoomsService(factory.CreateClient("Api"));
                }
            });

            // Infrastructure services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IToolbarService, ToolbarService>();
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();

            // ✅ ViewModels with GLOBAL static caching
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<LoginViewModel>();

            services.AddSingleton<DashboardViewModel>(sp =>
            {
                if (ViewModelCache.DashboardInstance == null)
                {
                    ViewModelCache.DashboardInstance = ActivatorUtilities.CreateInstance<DashboardViewModel>(sp);
                }
                return ViewModelCache.DashboardInstance;
            });

            services.AddSingleton<FrontDeskViewModel>(sp =>
            {
                if (ViewModelCache.FrontDeskInstance == null)
                {
                    ViewModelCache.FrontDeskInstance = ActivatorUtilities.CreateInstance<FrontDeskViewModel>(sp);
                }
                return ViewModelCache.FrontDeskInstance;
            });

            services.AddSingleton<HousekeepingViewModel>(sp =>
            {
                if (ViewModelCache.HousekeepingInstance == null)
                {
                    var hkService = sp.GetRequiredService<IHousekeepingService>();
                    var nav = sp.GetRequiredService<INavigationService>();
                    var toolbar = sp.GetRequiredService<IToolbarService>();
                    ViewModelCache.HousekeepingInstance = new HousekeepingViewModel(hkService, nav, toolbar);
                    ViewModelCache.HousekeepingInstance.StatusMessage = $"✅ CREATED GLOBAL - Hash: {ViewModelCache.HousekeepingInstance.GetHashCode()}";
                }
                else
                {
                    ViewModelCache.HousekeepingInstance.StatusMessage = $"✅ REUSED GLOBAL - Hash: {ViewModelCache.HousekeepingInstance.GetHashCode()} | Rooms: {ViewModelCache.HousekeepingInstance.Rooms.Count}";
                }
                return ViewModelCache.HousekeepingInstance;
            });

            services.AddSingleton<ProfileViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<FolioViewModel>();

            return services;
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

        public System.Threading.Tasks.Task StartAsync(
            System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task StopAsync(
            System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }
}
