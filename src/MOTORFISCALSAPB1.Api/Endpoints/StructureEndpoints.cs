using Microsoft.AspNetCore.SignalR;
using MOTORFISCALSAPB1.Api.Hubs;
using MOTORFISCALSAPB1.Application.UseCases;
using MOTORFISCALSAPB1.Shared.Enums;

namespace MOTORFISCALSAPB1.Api.Endpoints;

public static class StructureEndpoints
{
    public static IEndpointRouteBuilder MapStructureEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/structure").WithTags("Structure");

        g.MapPost("/validate", async (
            IInstallStructureUseCase uc,
            IConfiguration cfg,
            CancellationToken ct) =>
        {
            var path = cfg["Structure:ManifestPath"] ?? "manifests/structure.json";
            var report = await uc.ExecuteFromFileAsync(path, StructureExecutionMode.Validation, null, ct);
            return Results.Ok(report);
        }).WithName("ValidateStructure");

        g.MapPost("/install", async (
            IInstallStructureUseCase uc,
            IConfiguration cfg,
            IHubContext<StructureHub> hub,
            CancellationToken ct) =>
        {
            var path = cfg["Structure:ManifestPath"] ?? "manifests/structure.json";
            var progress = new StructureHubProgress(hub);
            var report = await uc.ExecuteFromFileAsync(path, StructureExecutionMode.Execution, progress, ct);
            return Results.Ok(report);
        }).WithName("InstallStructure");

        return app;
    }
}
