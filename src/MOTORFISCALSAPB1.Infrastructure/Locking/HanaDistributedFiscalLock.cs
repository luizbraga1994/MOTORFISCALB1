using Dapper;
using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Infrastructure.Persistence;

namespace MOTORFISCALSAPB1.Infrastructure.Locking;

/// <summary>
/// Lock distribuído persistido em <c>@MF_LOCK</c>. Baseado em INSERT com chave única:
/// a primeira requisição vence; as outras aguardam liberação por polling com backoff.
/// Locks expiram via TTL (<c>U_EXPIRES_AT</c>) — um DELETE oportunista remove locks vencidos.
/// </summary>
public sealed class HanaDistributedFiscalLock : IDistributedFiscalLock
{
    private readonly IHanaConnectionFactory _factory;
    private readonly ILogger<HanaDistributedFiscalLock> _logger;

    public HanaDistributedFiscalLock(IHanaConnectionFactory factory, ILogger<HanaDistributedFiscalLock> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        var owner = Guid.NewGuid().ToString("N");
        var attempt = 0;
        var maxAttempts = 50;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            attempt++;

            await CleanupExpiredAsync(ct).ConfigureAwait(false);

            if (await TryInsertAsync(key, owner, ttl, ct).ConfigureAwait(false))
            {
                return new Handle(this, key, owner);
            }

            if (attempt >= maxAttempts)
            {
                throw new TimeoutException($"Não foi possível adquirir lock '{key}' após {attempt} tentativas.");
            }

            var delayMs = Math.Min(100 * attempt, 1500);
            await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }
    }

    private async Task<bool> TryInsertAsync(string key, string owner, TimeSpan ttl, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO {0}.""@MF_LOCK"" (""Code"", ""Name"", ""U_OWNER"", ""U_EXPIRES_AT"", ""U_CREATED_AT"")
VALUES (:key, :key, :owner, :exp, CURRENT_UTCTIMESTAMP)";

        using var cn = _factory.Create();
        cn.Open();
        try
        {
            await cn.ExecuteAsync(new CommandDefinition(
                string.Format(sql, _factory.QuotedSchema),
                new { key, owner, exp = DateTime.UtcNow.Add(ttl) },
                cancellationToken: ct));
            return true;
        }
        catch (Exception ex)
        {
            // Chave duplicada => lock já ocupado. Qualquer outro erro é propagado.
            if (IsDuplicateKey(ex))
            {
                return false;
            }
            throw;
        }
    }

    private static bool IsDuplicateKey(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("unique", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("primary key", StringComparison.OrdinalIgnoreCase);
    }

    private async Task CleanupExpiredAsync(CancellationToken ct)
    {
        const string sql = @"DELETE FROM {0}.""@MF_LOCK"" WHERE ""U_EXPIRES_AT"" < CURRENT_UTCTIMESTAMP";
        try
        {
            using var cn = _factory.Create();
            cn.Open();
            await cn.ExecuteAsync(new CommandDefinition(string.Format(sql, _factory.QuotedSchema), cancellationToken: ct));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Cleanup de locks expirados falhou silenciosamente.");
        }
    }

    private async Task ReleaseAsync(string key, string owner)
    {
        const string sql = @"DELETE FROM {0}.""@MF_LOCK"" WHERE ""Code"" = :key AND ""U_OWNER"" = :owner";
        try
        {
            using var cn = _factory.Create();
            cn.Open();
            await cn.ExecuteAsync(new CommandDefinition(
                string.Format(sql, _factory.QuotedSchema),
                new { key, owner }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao liberar lock {Key}.", key);
        }
    }

    private sealed class Handle : IAsyncDisposable
    {
        private readonly HanaDistributedFiscalLock _owner;
        private readonly string _key;
        private readonly string _ownerId;
        private bool _released;

        public Handle(HanaDistributedFiscalLock owner, string key, string ownerId)
        {
            _owner = owner;
            _key = key;
            _ownerId = ownerId;
        }

        public async ValueTask DisposeAsync()
        {
            if (_released) return;
            _released = true;
            await _owner.ReleaseAsync(_key, _ownerId).ConfigureAwait(false);
        }
    }
}
