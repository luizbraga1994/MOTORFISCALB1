using System.Globalization;
using MOTORFISCALSAPB1.Domain.Common;
using MOTORFISCALSAPB1.Shared.Helpers;

namespace MOTORFISCALSAPB1.Domain.Fiscal;

/// <summary>
/// Assinatura fiscal determinística. Combina os atributos tributários em um
/// identificador único (hash) usado para mapear para TaxCode SAP e garantir idempotência.
/// </summary>
public sealed class FiscalSignature : ValueObject
{
    public string Cfop { get; }
    public string CstIcms { get; }
    public decimal AliquotaIcms { get; }
    public string CstIpi { get; }
    public decimal AliquotaIpi { get; }
    public string CstPis { get; }
    public decimal AliquotaPis { get; }
    public string CstCofins { get; }
    public decimal AliquotaCofins { get; }
    public bool TemSt { get; }
    public decimal? AliquotaStInterna { get; }
    public decimal? MvaSt { get; }

    public string Hash { get; }
    public string Canonical { get; }

    public FiscalSignature(
        string cfop,
        string cstIcms,
        decimal aliquotaIcms,
        string cstIpi,
        decimal aliquotaIpi,
        string cstPis,
        decimal aliquotaPis,
        string cstCofins,
        decimal aliquotaCofins,
        bool temSt,
        decimal? aliquotaStInterna,
        decimal? mvaSt)
    {
        if (string.IsNullOrWhiteSpace(cfop)) throw new DomainException("CFOP obrigatório.", "SIG_CFOP_REQUIRED");
        if (string.IsNullOrWhiteSpace(cstIcms)) throw new DomainException("CST ICMS obrigatório.", "SIG_CSTICMS_REQUIRED");

        Cfop = cfop.Trim();
        CstIcms = cstIcms.Trim();
        AliquotaIcms = Normalize(aliquotaIcms);
        CstIpi = (cstIpi ?? string.Empty).Trim();
        AliquotaIpi = Normalize(aliquotaIpi);
        CstPis = (cstPis ?? string.Empty).Trim();
        AliquotaPis = Normalize(aliquotaPis);
        CstCofins = (cstCofins ?? string.Empty).Trim();
        AliquotaCofins = Normalize(aliquotaCofins);
        TemSt = temSt;
        AliquotaStInterna = aliquotaStInterna.HasValue ? Normalize(aliquotaStInterna.Value) : (decimal?)null;
        MvaSt = mvaSt.HasValue ? Normalize(mvaSt.Value) : (decimal?)null;

        Canonical = BuildCanonical();
        Hash = FiscalHash.Sha256(Canonical);
    }

    private static decimal Normalize(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    private string BuildCanonical()
    {
        var c = CultureInfo.InvariantCulture;
        var st = TemSt ? "1" : "0";
        var aliqSt = AliquotaStInterna?.ToString("0.0000", c) ?? "-";
        var mva = MvaSt?.ToString("0.0000", c) ?? "-";
        return string.Join('|',
            Cfop,
            CstIcms, AliquotaIcms.ToString("0.0000", c),
            CstIpi, AliquotaIpi.ToString("0.0000", c),
            CstPis, AliquotaPis.ToString("0.0000", c),
            CstCofins, AliquotaCofins.ToString("0.0000", c),
            st, aliqSt, mva);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }

    public override string ToString() => Hash;
}
