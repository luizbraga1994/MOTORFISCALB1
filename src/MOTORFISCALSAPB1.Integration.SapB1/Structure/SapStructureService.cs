using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Shared.Contracts;
using MOTORFISCALSAPB1.Shared.Enums;
using Polly;
using Polly.Retry;

namespace MOTORFISCALSAPB1.Integration.SapB1.Structure;

/// <summary>
/// Orquestra a criação de UDTs, UDFs e UDOs conforme o manifesto.
/// Suporta modo Validação (sem efeitos colaterais) e Execução,
/// com retry e tolerância a falhas pontuais.
/// </summary>
public sealed class SapStructureService : ISapStructureService
{
    private readonly IManifestValidator _validator;
    private readonly IUserTablesMdService _tables;
    private readonly IUserFieldsMdService _fields;
    private readonly IUserObjectsMdService _objects;
    private readonly ILogger<SapStructureService> _logger;

    private readonly AsyncRetryPolicy _retry;

    public SapStructureService(
        IManifestValidator validator,
        IUserTablesMdService tables,
        IUserFieldsMdService fields,
        IUserObjectsMdService objects,
        ILogger<SapStructureService> logger)
    {
        _validator = validator;
        _tables = tables;
        _fields = fields;
        _objects = objects;
        _logger = logger;

        _retry = Policy
            .Handle<Exception>(ex => !(ex is OperationCanceledException))
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt)),
                (ex, ts, attempt, _) =>
                {
                    _logger.LogWarning(ex, "Retry {Attempt} em {Delay}ms", attempt, ts.TotalMilliseconds);
                });
    }

    public async Task<StructureExecutionReport> ExecuteAsync(
        StructureManifest manifest,
        StructureExecutionMode mode,
        IProgress<StructureProgressMessage>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var report = new StructureExecutionReport();
        var sw = Stopwatch.StartNew();

        Report(progress, "validation", "manifest", 0, 1, "running", "Validando manifesto.");
        var val = _validator.Validate(manifest);
        report.Warnings.AddRange(val.Warnings);
        if (!val.IsValid)
        {
            report.Errors.AddRange(val.Errors);
            Report(progress, "validation", "manifest", 1, 1, "failed", "Manifesto inválido.");
            report.Duration = sw.Elapsed;
            return report;
        }

        Report(progress, "validation", "manifest", 1, 1, "completed", "Manifesto válido.");

        if (mode == StructureExecutionMode.Validation)
        {
            report.Duration = sw.Elapsed;
            return report;
        }

        // 1) UDTs
        var idx = 0;
        foreach (var t in manifest.UserTables)
        {
            ct.ThrowIfCancellationRequested();
            idx++;
            Report(progress, "udt", t.Name, idx, manifest.UserTables.Count, "running");
            try
            {
                var created = await _retry.ExecuteAsync(c => _tables.EnsureAsync(t, c), ct).ConfigureAwait(false);
                if (created) report.TablesCreated++; else report.TablesSkipped++;
                Report(progress, "udt", t.Name, idx, manifest.UserTables.Count, "completed");
            }
            catch (Exception ex)
            {
                var msg = $"UDT {t.Name}: {ex.Message}";
                _logger.LogError(ex, "Falha na UDT {Name}", t.Name);
                report.Errors.Add(msg);
                Report(progress, "udt", t.Name, idx, manifest.UserTables.Count, "failed", msg);
                // Continua — falha pontual não aborta todo o processo.
            }
        }

        // 2) UDFs
        idx = 0;
        foreach (var f in manifest.UserFields)
        {
            ct.ThrowIfCancellationRequested();
            idx++;
            var label = $"{f.Table}.U_{f.Name.TrimStart('U', '_')}";
            Report(progress, "udf", label, idx, manifest.UserFields.Count, "running");
            try
            {
                var created = await _retry.ExecuteAsync(c => _fields.EnsureAsync(f, c), ct).ConfigureAwait(false);
                if (created) report.FieldsCreated++; else report.FieldsSkipped++;
                Report(progress, "udf", label, idx, manifest.UserFields.Count, "completed");
            }
            catch (Exception ex)
            {
                var msg = $"UDF {label}: {ex.Message}";
                _logger.LogError(ex, "Falha na UDF {Label}", label);
                report.Errors.Add(msg);
                Report(progress, "udf", label, idx, manifest.UserFields.Count, "failed", msg);
            }
        }

        // 3) UDOs
        idx = 0;
        foreach (var o in manifest.UserObjects)
        {
            ct.ThrowIfCancellationRequested();
            idx++;
            Report(progress, "udo", o.Code, idx, manifest.UserObjects.Count, "running");
            try
            {
                var created = await _retry.ExecuteAsync(c => _objects.EnsureAsync(o, c), ct).ConfigureAwait(false);
                if (created) report.ObjectsCreated++; else report.ObjectsSkipped++;
                Report(progress, "udo", o.Code, idx, manifest.UserObjects.Count, "completed");
            }
            catch (Exception ex)
            {
                var msg = $"UDO {o.Code}: {ex.Message}";
                _logger.LogError(ex, "Falha no UDO {Code}", o.Code);
                report.Errors.Add(msg);
                Report(progress, "udo", o.Code, idx, manifest.UserObjects.Count, "failed", msg);
            }
        }

        report.Duration = sw.Elapsed;
        return report;
    }

    private static void Report(
        IProgress<StructureProgressMessage>? p,
        string stage, string target, int current, int total, string status, string? message = null)
    {
        p?.Report(new StructureProgressMessage
        {
            Stage = stage,
            Target = target,
            Current = current,
            Total = total,
            Status = status,
            Message = message
        });
    }
}
