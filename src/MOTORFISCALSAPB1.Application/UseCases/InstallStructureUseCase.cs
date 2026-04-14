using System.Text.Json;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Shared.Contracts;
using MOTORFISCALSAPB1.Shared.Enums;

namespace MOTORFISCALSAPB1.Application.UseCases;

public interface IInstallStructureUseCase
{
    Task<StructureExecutionReport> ExecuteFromFileAsync(
        string manifestPath,
        StructureExecutionMode mode,
        IProgress<StructureProgressMessage>? progress,
        CancellationToken ct);

    Task<StructureExecutionReport> ExecuteAsync(
        StructureManifest manifest,
        StructureExecutionMode mode,
        IProgress<StructureProgressMessage>? progress,
        CancellationToken ct);
}

public sealed class InstallStructureUseCase : IInstallStructureUseCase
{
    private readonly ISapStructureService _service;

    public InstallStructureUseCase(ISapStructureService service)
    {
        _service = service;
    }

    public async Task<StructureExecutionReport> ExecuteFromFileAsync(
        string manifestPath,
        StructureExecutionMode mode,
        IProgress<StructureProgressMessage>? progress,
        CancellationToken ct)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Manifesto não encontrado: {manifestPath}", manifestPath);
        }

        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<StructureManifest>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Manifesto inválido.");

        return await _service.ExecuteAsync(manifest, mode, progress, ct).ConfigureAwait(false);
    }

    public Task<StructureExecutionReport> ExecuteAsync(
        StructureManifest manifest,
        StructureExecutionMode mode,
        IProgress<StructureProgressMessage>? progress,
        CancellationToken ct)
        => _service.ExecuteAsync(manifest, mode, progress, ct);
}
