namespace MOTORFISCALSAPB1.Domain.Fiscal;

/// <summary>
/// Resultado de resolução fiscal produzido pelo motor.
/// </summary>
public class FiscalResolutionResult
{
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
    public string? TaxCode { get; set; }
    public string AssinaturaFiscal { get; set; } = string.Empty;
    public bool TaxCodeCriadoAgora { get; set; }
}
