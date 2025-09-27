using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.ViewModels;
using KeyCard.Desktop.Views;


namespace KeyCard.Desktop.Infrastructure;

public static class Bootstrap
{
    public static IHost BuildHost(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                var apiBase = ctx.Configuration["Api:BaseUrl"] ?? "https://localhost:5001";
                services.AddHttpClient("KeyCardApi", client =>
                {
                    client.BaseAddress = new Uri(apiBase);
                    client.Timeout = TimeSpan.FromSeconds(20);
                });

                var useMocks = ctx.Configuration.GetValue("UseMocks", true);
                if (useMocks)
                {
                    services.AddSingleton<IAuthService, MockAuthService>();
                    services.AddSingleton<IBookingService, MockBookingService>();
                    services.AddSingleton<IHousekeepingService, MockHousekeepingService>();
                }
                else
                {
                    services.AddHttpClient<IAuthService, ApiAuthService>("KeyCardApi");
                    services.AddHttpClient<IBookingService, ApiBookingService>("KeyCardApi");
                    services.AddHttpClient<IHousekeepingService, ApiHousekeepingService>("KeyCardApi");
                }

                services.AddSingleton<INavigationService, NavigationService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<FrontDeskViewModel>();
                services.AddTransient<HousekeepingViewModel>();

                // Views
                services.AddTransient<LoginView>();
                services.AddTransient<DashboardView>();
                services.AddTransient<FrontDeskView>();
                services.AddTransient<HousekeepingView>();
            })
            .Build();
    }
}
