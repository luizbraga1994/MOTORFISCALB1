namespace MOTORFISCALSAPB1.Application.Abstractions;

/// <summary>
/// Lock distribuído usado para serializar criação de TaxCode por assinatura (persistido em <c>@MF_LOCK</c>).
/// </summary>
public interface IDistributedFiscalLock
{
    Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct);
}
