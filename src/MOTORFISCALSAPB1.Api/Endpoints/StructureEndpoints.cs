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
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var path = ResolveManifestPath(cfg, env);
            var report = await uc.ExecuteFromFileAsync(path, StructureExecutionMode.Validation, null, ct);
            return Results.Ok(report);
        }).WithName("ValidateStructure");

        g.MapPost("/install", async (
            IInstallStructureUseCase uc,
            IConfiguration cfg,
            IHostEnvironment env,
            IHubContext<StructureHub> hub,
            CancellationToken ct) =>
        {
            var path = ResolveManifestPath(cfg, env);
            var progress = new StructureHubProgress(hub);
            var report = await uc.ExecuteFromFileAsync(path, StructureExecutionMode.Execution, progress, ct);
            return Results.Ok(report);
        }).WithName("InstallStructure");

        return app;
    }

    // Resolve o caminho do manifesto. Se for relativo, combina com o
    // ContentRootPath (que e o diretorio de publish/output do build).
    // Assim funciona tanto em dotnet run/F5 quanto em publish deployado.
    private static string ResolveManifestPath(IConfiguration cfg, IHostEnvironment env)
    {
        var path = cfg["Structure:ManifestPath"] ?? "manifests/structure.json";
        return Path.IsPathRooted(path) ? path : Path.Combine(env.ContentRootPath, path);
    }
}
