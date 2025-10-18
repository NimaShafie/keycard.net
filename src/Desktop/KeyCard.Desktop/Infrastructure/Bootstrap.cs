// Infrastructure/Bootstrap.cs
using System;
using System.Net.Http.Headers;
using System.Net.Http; // for HttpResponseMessage

using KeyCard.Desktop.Generated;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Services.Api;
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

            // API client
            services.AddSingleton(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new KeyCardApiClient(factory.CreateClient("Api"));
            });

            // SignalR
            services.AddSingleton<ISignalRService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();

                if (env.IsMock || string.IsNullOrWhiteSpace(env.BookingsHubUrl))
                    return new NoOpSignalRService();

                try
                {
                    // Use the LIVE SignalR service if available
                    return ActivatorUtilities.CreateInstance<global::KeyCard.Desktop.Services.Live.SignalRService>(sp, env.BookingsHubUrl);
                }
                catch
                {
                    // Fall back gracefully so the app can still boot
                    return new NoOpSignalRService();
                }
            });

            // Business services
            services.AddSingleton<IBookingService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                return env.IsMock
                    ? new global::KeyCard.Desktop.Services.Mock.BookingService()
                    : new global::KeyCard.Desktop.Services.Api.BookingService(
                        sp.GetRequiredService<global::KeyCard.Desktop.Generated.KeyCardApiClient>());
            });

            services.AddSingleton<IHousekeepingService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                return env.IsMock
                    ? new global::KeyCard.Desktop.Services.Mock.HousekeepingService()
                    : ActivatorUtilities.CreateInstance<global::KeyCard.Desktop.Services.Api.HousekeepingService>(sp);
            });

            services.AddSingleton<IAuthService>(sp =>
            {
                var env = sp.GetRequiredService<IAppEnvironment>();
                return env.IsMock
                    ? new global::KeyCard.Desktop.Services.Mock.AuthService()
                    : new global::KeyCard.Desktop.Services.Api.AuthService();
            });

            // Infrastructure services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<FrontDeskViewModel>();
            services.AddTransient<HousekeepingViewModel>();
            services.AddTransient<ProfileViewModel>();
            services.AddTransient<SettingsViewModel>();

            return services;
        }
    }

    // No-op SignalR for mock mode
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
