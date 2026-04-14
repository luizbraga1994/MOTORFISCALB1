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

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddHealthChecks();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
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
