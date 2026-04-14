using MOTORFISCALSAPB1.Domain.Fiscal;
using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Application.Abstractions;

/// <summary>
/// Constrói o <see cref="FiscalResolutionContext"/> a partir do request,
/// enriquecendo com dados reais do SAP (OCRD/CRD1/CRD7/OBPL/OITM).
/// </summary>
public interface IFiscalContextBuilder
{
    Task<FiscalResolutionContext> BuildAsync(FiscalResolutionRequest request, CancellationToken ct);
}
