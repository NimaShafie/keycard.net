public class ContractsVersionMiddleware
{
    private readonly RequestDelegate _next;
    public const string HeaderName = "X-KeyCard-Contracts-Version";
    public const string Current = "2025-09-27";

    public ContractsVersionMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        ctx.Response.OnStarting(() =>
        {
            if (!ctx.Response.Headers.ContainsKey(HeaderName))
                ctx.Response.Headers[HeaderName] = Current;
            return Task.CompletedTask;
        });
        await _next(ctx);
    }
}
