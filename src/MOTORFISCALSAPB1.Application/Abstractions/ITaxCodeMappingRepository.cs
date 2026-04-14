using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Abstractions;

public interface ITaxCodeMappingRepository
{
    Task<TaxCodeMapping?> GetByHashAsync(string signatureHash, CancellationToken ct);
    Task AddAsync(TaxCodeMapping mapping, CancellationToken ct);
}
