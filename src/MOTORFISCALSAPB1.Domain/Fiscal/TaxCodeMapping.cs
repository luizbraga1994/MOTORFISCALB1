using MOTORFISCALSAPB1.Domain.Common;

namespace MOTORFISCALSAPB1.Domain.Fiscal;

/// <summary>
/// Vínculo persistido em <c>@MF_TAXMAP</c>: assinatura fiscal ↔ TaxCode SAP.
/// </summary>
public class TaxCodeMapping : Entity<string>
{
    public string SignatureHash { get; private set; } = string.Empty;
    public string TaxCode { get; private set; } = string.Empty;
    public string Canonical { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }

    private TaxCodeMapping() : base(string.Empty) { }

    public TaxCodeMapping(string signatureHash, string taxCode, string canonical, string? createdBy = null)
        : base(signatureHash)
    {
        if (string.IsNullOrWhiteSpace(signatureHash))
            throw new DomainException("SignatureHash é obrigatório.", "MAP_HASH_REQUIRED");
        if (string.IsNullOrWhiteSpace(taxCode))
            throw new DomainException("TaxCode é obrigatório.", "MAP_TAXCODE_REQUIRED");

        SignatureHash = signatureHash;
        TaxCode = taxCode;
        Canonical = canonical;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }
}
