using System.Net;

namespace MOTORFISCALSAPB1.Api.Middleware;

/// <summary>
/// Autenticacao minima por API key compartilhada entre Addon e API.
/// A key e lida de configuration (Security:ApiKey) ou variavel de ambiente
/// MF_API_KEY. Se nao estiver configurada, o middleware permite tudo e emite
/// log Warning no startup (modo dev). Em producao, falha na ausencia da key
/// configurada resulta em 401 para toda requisicao sem o header esperado.
///
/// Rotas isentas: /health, /hubs/** (SignalR usa query string), /swagger/**.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware
{
    public const string HeaderName = "X-Api-Key";

    private static readonly string[] ExemptPrefixes =
    {
        "/health",
        "/swagger",
        "/hubs"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private readonly string? _configuredKey;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration cfg,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _configuredKey = cfg["Security:ApiKey"]
                         ?? Environment.GetEnvironmentVariable("MF_API_KEY");

        if (string.IsNullOrWhiteSpace(_configuredKey))
        {
            _logger.LogWarning(
                "API key nao configurada (Security:ApiKey ou MF_API_KEY). " +
                "Endpoints ficam abertos — NAO USAR EM PRODUCAO.");
        }
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        // Sem key configurada = modo dev, passa tudo.
        if (string.IsNullOrWhiteSpace(_configuredKey))
        {
            await _next(ctx).ConfigureAwait(false);
            return;
        }

        var path = ctx.Request.Path.Value ?? string.Empty;
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next(ctx).ConfigureAwait(false);
                return;
            }
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var provided)
            || !string.Equals(provided.ToString(), _configuredKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Requisicao {Method} {Path} sem API key valida. Remote={Remote}",
                ctx.Request.Method, path, ctx.Connection.RemoteIpAddress);
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"error\":\"API key obrigatoria.\",\"code\":\"AUTH_API_KEY_MISSING\"}")
                .ConfigureAwait(false);
            return;
        }

        await _next(ctx).ConfigureAwait(false);
    }
}
