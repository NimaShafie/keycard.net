// Services/ServiceRegistration.cs
using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.Services;

/// <summary>
/// Central place to register Desktop DI, honoring KeyCard:Mode and KeyCard:UseMocks.
/// Call services.AddKeyCardDesktopServices(configuration);
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddKeyCardDesktopServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind API route options for consumers (e.g., BookingService)
        services.Configure<ApiRoutesOptions>(configuration.GetSection("Api:Routes"));

        // Decide base URL from configuration
        var https = configuration["KeyCard:Api:HttpsBaseUrl"]?.Trim();
        var http = configuration["KeyCard:Api:HttpBaseUrl"]?.Trim();
        var apiBase = !string.IsNullOrWhiteSpace(https) ? https
                    : !string.IsNullOrWhiteSpace(http) ? http
                    : null;

        if (string.IsNullOrWhiteSpace(apiBase))
            throw new InvalidOperationException("KeyCard:Api base URL is not configured (HttpsBaseUrl/HttpBaseUrl).");

        // Named HttpClient used by LIVE services
        services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBase));

        // MOCK vs LIVE switch (read directly from config)
        var mode = configuration["KeyCard:Mode"] ?? "Live";
        var useMocksStr = configuration["KeyCard:UseMocks"];
        var useMocks = false;
        if (!string.IsNullOrWhiteSpace(useMocksStr))
            _ = bool.TryParse(useMocksStr, out useMocks);

        var isMock = useMocks || mode.Equals("Mock", StringComparison.OrdinalIgnoreCase);

        if (isMock)
        {
            services.AddSingleton<IBookingService, MockBookingService>();
        }
        else
        {
            services.AddSingleton<IBookingService, BookingService>();
        }

        // NOTE: We intentionally DO NOT register IAppEnvironment here to avoid
        // colliding with your existing Services/IAppEnvironment.cs + Program.cs wiring.

        return services;
    }
}
