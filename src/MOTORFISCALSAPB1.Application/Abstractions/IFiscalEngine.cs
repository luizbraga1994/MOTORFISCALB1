using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Abstractions;

public interface IFiscalEngine
{
    /// <summary>Resolve a regra e garante o TaxCode SAP, retornando o resultado completo.</summary>
    Task<FiscalResolutionResult> ResolveAsync(FiscalResolutionContext context, CancellationToken ct);

    /// <summary>Apenas simula: resolve regra e assinatura, mas não cria TaxCode.</summary>
    Task<FiscalResolutionResult> SimulateAsync(FiscalResolutionContext context, CancellationToken ct);
}
