// Modules/Folio/FolioModule.cs
using KeyCard.Desktop.Modules.Folio.Services;

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.Modules.Folio
{
    public static class FolioModule
    {
        public static IServiceCollection AddFolioModule(this IServiceCollection services, KeyCard.Desktop.Services.IAppEnvironment env)
        {
            if (env.IsMock)
                services.AddSingleton<IFolioService, MockFolioService>();
            else
                services.AddSingleton<IFolioService, LiveFolioService>();

            return services;
        }
    }
}
