using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Abstractions;

public interface IFiscalRuleRepository
{
    Task<IReadOnlyList<FiscalRule>> GetAllActiveAsync(CancellationToken ct);
    Task<FiscalRule?> GetByIdAsync(string id, CancellationToken ct);
    Task UpsertAsync(FiscalRule rule, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
}
