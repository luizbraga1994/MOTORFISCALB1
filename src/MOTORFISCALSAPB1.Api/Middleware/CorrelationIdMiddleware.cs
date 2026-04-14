using Serilog.Context;

namespace MOTORFISCALSAPB1.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers.TryGetValue(HeaderName, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : Guid.NewGuid().ToString("N");

        ctx.Response.Headers[HeaderName] = correlationId;
        ctx.Items["CorrelationId"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(ctx).ConfigureAwait(false);
        }
    }
}
