using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Abstractions;

public interface IFiscalAuditRepository
{
    Task AddAsync(FiscalAuditEntry entry, CancellationToken ct);
}
