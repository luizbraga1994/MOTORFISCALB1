namespace MOTORFISCALSAPB1.Shared.Contracts;

/// <summary>
/// Resposta com o resultado fiscal resolvido pelo motor.
/// </summary>
public class FiscalResolutionResponse
{
    public string TaxCode { get; set; } = string.Empty;
    public string Cfop { get; set; } = string.Empty;

    public string CstIcms { get; set; } = string.Empty;
    public decimal AliquotaIcms { get; set; }
    public decimal? ReducaoBaseIcms { get; set; }

    public string CstIpi { get; set; } = string.Empty;
    public decimal AliquotaIpi { get; set; }

    public string CstPis { get; set; } = string.Empty;
    public decimal AliquotaPis { get; set; }

    public string CstCofins { get; set; } = string.Empty;
    public decimal AliquotaCofins { get; set; }

    public bool TemSt { get; set; }
    public decimal? MvaSt { get; set; }
    public decimal? AliquotaStInterna { get; set; }

    public bool TemDifal { get; set; }
    public decimal? AliquotaDifal { get; set; }
    public decimal? AliquotaInterestadual { get; set; }

    public string RegraAplicada { get; set; } = string.Empty;
    public string AssinaturaFiscal { get; set; } = string.Empty;
    public bool TaxCodeCriadoAgora { get; set; }

    public string? CorrelationId { get; set; }
    public DateTime ResolvedAtUtc { get; set; } = DateTime.UtcNow;
}
