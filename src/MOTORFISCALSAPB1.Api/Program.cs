using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MOTORFISCALSAPB1.Api.Endpoints;
using MOTORFISCALSAPB1.Api.Hubs;
using MOTORFISCALSAPB1.Api.Middleware;
using MOTORFISCALSAPB1.Api.Validators;
using MOTORFISCALSAPB1.Application;
using MOTORFISCALSAPB1.Infrastructure;
using MOTORFISCALSAPB1.Integration.SapB1;
using Serilog;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // Quando executando como Windows Service, o CWD e' system32.
    // ContentRootPath fixa o diretorio do binario para localizar appsettings.
    ContentRootPath = AppContext.BaseDirectory
});

// Integracao com Windows Service Control Manager (SCM):
// - responde a START/STOP/SHUTDOWN
// - escreve no Windows Event Log quando nao consegue logar em arquivo
// - no-op quando rodando em modo console (desenvolvimento / Linux / Docker)
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "MOTORFISCALSAPB1.Api";
});

builder.Host.UseSerilog((ctx, _, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MOTORFISCALSAPB1 API",
        Version = "v1",
        Description = "Motor fiscal inteligente integrado ao SAP Business One."
    });
});

builder.Services.AddSignalR();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSapB1Integration(builder.Configuration);

builder.Services.AddScoped<IValidator<MOTORFISCALSAPB1.Shared.Contracts.FiscalResolutionRequest>, FiscalResolutionRequestValidator>();
builder.Services.AddScoped<IValidator<MOTORFISCALSAPB1.Shared.Contracts.TaxCodeEnsureRequest>, TaxCodeEnsureRequestValidator>();
builder.Services.AddScoped<IValidator<MOTORFISCALSAPB1.Api.Endpoints.FiscalRuleDto>, FiscalRuleDtoValidator>();

builder.Services.AddHealthChecks();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
// Auth vem apos exception handling e correlation id para que erros 401 ja
// tenham CorrelationId no header e sejam capturaveis no log de request.
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();

app.MapFiscalEndpoints();
app.MapRuleEndpoints();
app.MapStructureEndpoints();

app.MapHub<StructureHub>(StructureHub.Path);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

public partial class Program { }
