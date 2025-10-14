using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.Infrastructure.Hubs.RealTime;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Infrastructure.Helper
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRealtime(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, NameIdUserIdProvider>();
            services.AddSingleton<ConnectionRegistry>();

            // Allow SignalR to read JWT from ?access_token=
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Events ??= new JwtBearerEvents();
                var prev = options.Events.OnMessageReceived;
                options.Events.OnMessageReceived = async ctx =>
                {
                    if (prev is not null) await prev(ctx);
                    if (!string.IsNullOrEmpty(ctx.Token)) return;
                    var token = ctx.Request.Query["access_token"];
                    var path = ctx.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                        ctx.Token = token;
                };
            });

            // Broadcasters
            services.AddScoped<IBookingsRealtime, BookingsRealtime>();

            return services;
        }
    }
}
