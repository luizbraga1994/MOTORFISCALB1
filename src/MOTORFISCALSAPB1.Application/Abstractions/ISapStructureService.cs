using MOTORFISCALSAPB1.Shared.Contracts;
using MOTORFISCALSAPB1.Shared.Enums;

namespace MOTORFISCALSAPB1.Application.Abstractions;

/// <summary>
/// Serviço de criação/validação de estrutura SAP via Service Layer (UDT/UDF/UDO).
/// </summary>
public interface ISapStructureService
{
    Task<StructureExecutionReport> ExecuteAsync(
        StructureManifest manifest,
        StructureExecutionMode mode,
        IProgress<StructureProgressMessage>? progress,
        CancellationToken ct);
}

public class StructureExecutionReport
{
    public int TablesCreated { get; set; }
    public int TablesSkipped { get; set; }
    public int FieldsCreated { get; set; }
    public int FieldsSkipped { get; set; }
    public int ObjectsCreated { get; set; }
    public int ObjectsSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan Duration { get; set; }
}
