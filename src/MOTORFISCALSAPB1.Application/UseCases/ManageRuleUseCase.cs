using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Application.Services;
using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.UseCases;

public interface IManageRuleUseCase
{
    Task<IReadOnlyList<FiscalRule>> ListAsync(CancellationToken ct);
    Task<FiscalRule?> GetAsync(string id, CancellationToken ct);
    Task UpsertAsync(FiscalRule rule, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
}

public sealed class ManageRuleUseCase : IManageRuleUseCase
{
    private readonly IFiscalRuleRepository _repo;
    private readonly IFiscalRuleCache _cache;

    public ManageRuleUseCase(IFiscalRuleRepository repo, IFiscalRuleCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public Task<IReadOnlyList<FiscalRule>> ListAsync(CancellationToken ct) => _repo.GetAllActiveAsync(ct);

    public Task<FiscalRule?> GetAsync(string id, CancellationToken ct) => _repo.GetByIdAsync(id, ct);

    public async Task UpsertAsync(FiscalRule rule, CancellationToken ct)
    {
        await _repo.UpsertAsync(rule, ct).ConfigureAwait(false);
        _cache.Invalidate();
    }

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct).ConfigureAwait(false);
        _cache.Invalidate();
    }
}
