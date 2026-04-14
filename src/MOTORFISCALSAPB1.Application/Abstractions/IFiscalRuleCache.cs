using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Abstractions;

public interface IFiscalRuleCache
{
    Task<IReadOnlyList<FiscalRule>> GetOrLoadAsync(
        Func<CancellationToken, Task<IReadOnlyList<FiscalRule>>> loader,
        CancellationToken ct);

    void Invalidate();
}
