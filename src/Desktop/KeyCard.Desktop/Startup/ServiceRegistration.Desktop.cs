// Startup/ServiceRegistration.Desktop.cs
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Services.Live;
using KeyCard.Desktop.Services.Mock;

using LiveRoomsService = KeyCard.Desktop.Services.Live.RoomsService;
using MockRoomsService = KeyCard.Desktop.Services.Mock.RoomsService;


namespace KeyCard.Desktop.Startup
{
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers IRoomsService in Mock or Live mode.
        /// Expects an IHttpClientFactory already configured for your API base address,
        /// or will create a bare HttpClient if not found.
        /// </summary>
        public static IServiceCollection AddRoomsService(
            this IServiceCollection services,
            IAppEnvironment appEnv // your existing abstraction with IsMock - already in the project
        )
        {
            if (appEnv is null) throw new ArgumentNullException(nameof(appEnv));

            if (appEnv.IsMock)
            {
                services.AddSingleton<IRoomsService, MockRoomsService>(); // Mock
            }
            else
            {
                // Prefer named client "Api" if you have one
                services.AddHttpClient<IRoomsService, LiveRoomsService>("Api", client =>
                {
                    // keep your existing BaseAddress setup (or leave as-is if set elsewhere)
                });
            }
            return services;
        }
    }
}
