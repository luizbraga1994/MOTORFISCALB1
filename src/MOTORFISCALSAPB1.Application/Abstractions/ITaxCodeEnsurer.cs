using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Abstractions;

/// <summary>
/// Garante a existência do TaxCode correspondente à assinatura fiscal.
/// Idempotente e thread-safe: concorrência é resolvida via <see cref="IDistributedFiscalLock"/>.
/// </summary>
public interface ITaxCodeEnsurer
{
    Task<TaxCodeEnsureOutcome> EnsureAsync(
        FiscalSignature signature,
        FiscalResolutionResult result,
        CancellationToken ct);
}

public sealed record TaxCodeEnsureOutcome(string TaxCode, bool Criado, string SignatureHash);
