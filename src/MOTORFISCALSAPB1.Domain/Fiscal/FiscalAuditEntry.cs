using MOTORFISCALSAPB1.Domain.Common;

namespace MOTORFISCALSAPB1.Domain.Fiscal;

/// <summary>
/// Entrada de auditoria persistida em <c>@MF_LOG</c>.
/// </summary>
public class FiscalAuditEntry : Entity<string>
{
    public string? CorrelationId { get; private set; }
    public string? UserName { get; private set; }
    public string? CardCode { get; private set; }
    public int? BplId { get; private set; }
    public string? ItemCode { get; private set; }
    public string? RuleId { get; private set; }
    public string? SignatureHash { get; private set; }
    public string? TaxCode { get; private set; }
    public bool TaxCodeCreated { get; private set; }
    public long DurationMs { get; private set; }
    public string Request { get; private set; } = string.Empty;
    public string Response { get; private set; } = string.Empty;
    public string? Error { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private FiscalAuditEntry() : base(Guid.NewGuid().ToString("N")) { }

    public FiscalAuditEntry(
        string id,
        string? correlationId,
        string? userName,
        string? cardCode,
        int? bplId,
        string? itemCode,
        string? ruleId,
        string? signatureHash,
        string? taxCode,
        bool taxCodeCreated,
        long durationMs,
        string request,
        string response,
        string? error)
        : base(id)
    {
        CorrelationId = correlationId;
        UserName = userName;
        CardCode = cardCode;
        BplId = bplId;
        ItemCode = itemCode;
        RuleId = ruleId;
        SignatureHash = signatureHash;
        TaxCode = taxCode;
        TaxCodeCreated = taxCodeCreated;
        DurationMs = durationMs;
        Request = request ?? string.Empty;
        Response = response ?? string.Empty;
        Error = error;
    }
}
