using Microsoft.Extensions.Caching.Memory;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Infrastructure.Caching;

public sealed class FiscalRuleCache : IFiscalRuleCache
{
    private const string Key = "mf::rules::active";
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FiscalRuleCache(IMemoryCache cache)
    {
        _cache = cache;
        _ttl = TimeSpan.FromMinutes(5);
    }

    public async Task<IReadOnlyList<FiscalRule>> GetOrLoadAsync(
        Func<CancellationToken, Task<IReadOnlyList<FiscalRule>>> loader,
        CancellationToken ct)
    {
        if (_cache.TryGetValue<IReadOnlyList<FiscalRule>>(Key, out var cached) && cached is not null)
        {
            return cached;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue<IReadOnlyList<FiscalRule>>(Key, out cached) && cached is not null)
            {
                return cached;
            }

            var rules = await loader(ct).ConfigureAwait(false);
            _cache.Set(Key, rules, _ttl);
            return rules;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() => _cache.Remove(Key);
}
