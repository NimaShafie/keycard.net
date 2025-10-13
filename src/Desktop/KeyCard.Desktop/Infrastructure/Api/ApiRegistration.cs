// Infrastructure/Api/ApiRegistration.cs
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Services.Live;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.Infrastructure.Api
{
    public static class ApiRegistration
    {
        public static IServiceCollection AddKeyCardApi(this IServiceCollection services, IConfiguration config, IAppEnvironment env)
        {
            // Bind route map
            var routes = new ApiRoutes();
            config.GetSection("Api:Routes").Bind(routes);
            services.AddSingleton(routes);

            // Named HttpClient (no Polly; simple and un-opinionated)
            services.AddHttpClient("Api");

            if (!env.IsMock)
            {
                // Register low-level API clients (do NOT implement your UI interfaces)
                services.AddSingleton<AuthApi>();
                services.AddSingleton<HotelsApi>();
                services.AddSingleton<RoomsApi>();
                services.AddSingleton<HousekeepingApi>();
                services.AddSingleton<BookingsApi>();
                services.AddSingleton<KeysApi>();
                services.AddSingleton<PaymentsApi>();
                services.AddSingleton<InvoicesApi>();
            }

            return services;
        }
    }
}
