namespace MOTORFISCALSAPB1.Domain.Sap;

/// <summary>
/// Representação forte do TaxCode a ser criado/consultado no SAP B1.
/// Usada pelos serviços de integração (Service Layer/DI API).
/// </summary>
public class TaxCodeDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>O (Output/Saída) ou I (Input/Entrada).</summary>
    public string Category { get; set; } = "O";
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
    public decimal? AliquotaStInterna { get; set; }
    public decimal? MvaSt { get; set; }

    public bool TemDifal { get; set; }
    public decimal? AliquotaDifal { get; set; }
    public decimal? AliquotaInterestadual { get; set; }
}
