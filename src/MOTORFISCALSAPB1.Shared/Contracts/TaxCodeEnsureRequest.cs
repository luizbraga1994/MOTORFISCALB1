namespace MOTORFISCALSAPB1.Shared.Contracts;

public class TaxCodeEnsureRequest
{
    public string Cfop { get; set; } = string.Empty;
    public string CstIcms { get; set; } = string.Empty;
    public decimal AliquotaIcms { get; set; }
    public string CstIpi { get; set; } = string.Empty;
    public decimal AliquotaIpi { get; set; }
    public string CstPis { get; set; } = string.Empty;
    public decimal AliquotaPis { get; set; }
    public string CstCofins { get; set; } = string.Empty;
    public decimal AliquotaCofins { get; set; }
    public bool TemSt { get; set; }
    public decimal? AliquotaStInterna { get; set; }
    public decimal? MvaSt { get; set; }
}

public class TaxCodeEnsureResponse
{
    public string TaxCode { get; set; } = string.Empty;
    public string AssinaturaFiscal { get; set; } = string.Empty;
    public bool Criado { get; set; }
}
