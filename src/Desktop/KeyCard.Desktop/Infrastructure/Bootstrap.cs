// Infrastructure/Bootstrap.cs
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using KeyCard.Desktop.Generated;
using KeyCard.Desktop.Modules.Folio.Services;
using KeyCard.Desktop.Modules.Folio.ViewModels;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.ViewModels;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Extensions.Http;

namespace KeyCard.Desktop.Infrastructure
{
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

            // Typed API client (stub until NSwag is enabled)
            services.AddSingleton(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new KeyCardApiClient(factory.CreateClient("Api"));
            });

            // SignalR
            services.AddSingleton<KeyCard.Desktop.Services.ISignalRService>(sp =>
            {
                var env = sp.GetRequiredService<KeyCard.Desktop.Services.IAppEnvironment>();
                return env.IsMock
                    ? new NoOpSignalRService()
                    : new KeyCard.Desktop.Services.SignalRService(env.BookingsHubUrl);
            });

            // Business services - Booking
            services.AddSingleton<KeyCard.Desktop.Services.IBookingService>(sp =>
            {
                var env = sp.GetRequiredService<KeyCard.Desktop.Services.IAppEnvironment>();
                if (env.IsMock)
                {
                    return new KeyCard.Desktop.Services.Mock.BookingService();
                }
                else
                {
                    // Adapter that uses IHttpClientFactory + RoutesOptions + ILogger
                    return ActivatorUtilities.CreateInstance<KeyCard.Desktop.Services.BookingService>(sp);
                }
            });

            // Business services - Housekeeping
            services.AddSingleton<KeyCard.Desktop.Services.IHousekeepingService>(sp =>
            {
                var env = sp.GetRequiredService<KeyCard.Desktop.Services.IAppEnvironment>();
                if (env.IsMock)
                {
                    return new KeyCard.Desktop.Services.Mock.HousekeepingService();
                }
                else
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var http = factory.CreateClient("Api");
                    return new KeyCard.Desktop.Services.Api.HousekeepingService(http);
                }
            });

            // Business services - Auth
            services.AddSingleton<KeyCard.Desktop.Services.IAuthService>(sp =>
            {
                var env = sp.GetRequiredService<KeyCard.Desktop.Services.IAppEnvironment>();
                return env.IsMock
                    ? new KeyCard.Desktop.Services.Mock.AuthService()
                    : new KeyCard.Desktop.Services.Api.AuthService();
            });

            // Business services - Folio
            services.AddSingleton<IFolioService>(sp =>
            {
                var env = sp.GetRequiredService<KeyCard.Desktop.Services.IAppEnvironment>();
                if (env.IsMock)
                {
                    return new MockFolioService();
                }
                else
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var http = factory.CreateClient("Api");
                    return new LiveFolioService(http);
                }
            });

            // Infrastructure services
            services.AddSingleton<KeyCard.Desktop.Services.INavigationService, KeyCard.Desktop.Services.NavigationService>();
            services.AddSingleton<KeyCard.Desktop.Services.IErrorHandlingService, KeyCard.Desktop.Services.ErrorHandlingService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<FrontDeskViewModel>();
            services.AddTransient<HousekeepingViewModel>();
            services.AddTransient<ProfileViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<FolioViewModel>();

            return services;
        }
    }

    // No-op SignalR for mock mode
    internal sealed class NoOpSignalRService : KeyCard.Desktop.Services.ISignalRService
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
