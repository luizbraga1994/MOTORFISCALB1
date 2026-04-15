using System.Net;
using System.Text.Json;
using FluentValidation;
using MOTORFISCALSAPB1.Domain.Common;
using MOTORFISCALSAPB1.Integration.SapB1.ServiceLayer;

namespace MOTORFISCALSAPB1.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            // FluentValidation: agrega todos os erros em array estavel.
            _logger.LogWarning("Validation error: {Errors}",
                string.Join("; ", ex.Errors.Select(e => e.PropertyName + ": " + e.ErrorMessage)));
            await WriteAsync(ctx, HttpStatusCode.BadRequest, new
            {
                error = "Requisicao invalida.",
                code = "VALIDATION_ERROR",
                details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }),
                correlationId = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error: {Code}", ex.Code);
            await WriteAsync(ctx, HttpStatusCode.BadRequest, new
            {
                error = ex.Message,
                code = ex.Code,
                correlationId = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            });
        }
        catch (ServiceLayerException ex)
        {
            _logger.LogError(ex, "Service Layer error");
            await WriteAsync(ctx, HttpStatusCode.BadGateway, new
            {
                error = "Falha na comunicação com o Service Layer.",
                detail = ex.ResponseBody,
                correlationId = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            });
        }
        catch (OperationCanceledException)
        {
            // Cliente cancelou — não logar como erro.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado");
            await WriteAsync(ctx, HttpStatusCode.InternalServerError, new
            {
                error = "Erro interno.",
                correlationId = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            });
        }
    }

    private static async Task WriteAsync(HttpContext ctx, HttpStatusCode status, object body)
    {
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))).ConfigureAwait(false);
    }
}
