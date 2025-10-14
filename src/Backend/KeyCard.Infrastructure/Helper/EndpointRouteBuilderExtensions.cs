using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.Infrastructure.Hubs;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace KeyCard.Infrastructure.Helper
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapRealtimeEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<BookingsHub>("/hubs/bookings");
            return endpoints;
        }
    }
}
