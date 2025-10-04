//Infrastructure/Bootstrap.cs
using System;
using System.Net.Http.Headers;

using KeyCard.Desktop.Generated;
using KeyCard.Desktop.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.Extensions.Http;

namespace KeyCard.Desktop.Infrastructure;

public static class Bootstrap
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services, IConfiguration cfg)
    {
        var apiBase = cfg["ApiBaseUrl"] ?? Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:8080";

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

        services.AddHttpClient("Api", c =>
        {
            c.BaseAddress = new Uri(apiBase);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler(() => new RetryDelegatingHandler(RetryPolicy()));

        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new KeyCardApiClient(factory.CreateClient("Api"));
        });

        services.AddSingleton<ISignalRService>(_ => new SignalRService(apiBase));

        // Swap Mock vs Api here if you want offline
        services.AddSingleton<IBookingService, ApiBookingService>();
        // services.AddSingleton<IBookingService, MockBookingService>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAuthService, ApiAuthService>();

        // ViewModels (add yours as needed)
        services.AddSingleton<ViewModels.MainViewModel>();
        services.AddSingleton<ViewModels.LoginViewModel>();
        services.AddSingleton<ViewModels.DashboardViewModel>();
        services.AddSingleton<ViewModels.FrontDeskViewModel>();
        services.AddSingleton<ViewModels.HousekeepingViewModel>();

        return services;
    }
}
