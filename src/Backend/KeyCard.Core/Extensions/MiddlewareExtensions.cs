using KeyCard.Core.Middleware;

using Microsoft.AspNetCore.Builder;

namespace KeyCard.Core.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseValidationExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ValidationExceptionMiddleware>();
        }
    }
}
