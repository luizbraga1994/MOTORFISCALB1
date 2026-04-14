namespace MOTORFISCALSAPB1.Domain.Sap;

/// <summary>
/// Projeção de dados do Business Partner (OCRD + CRD1 + CRD7).
/// </summary>
public class BusinessPartner
{
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    /// <summary>C, S ou L (OCRD.CardType).</summary>
    public string CardType { get; set; } = string.Empty;

    public string LicTradNum { get; set; } = string.Empty; // CNPJ/CPF (OCRD.LicTradNum)
    public string InscricaoEstadual { get; set; } = string.Empty; // CRD7.TaxId1 ou OCRD.MYFTaxId (depende da instalação)

    public bool ContribuinteIcms { get; set; }

    // NOTA: "Consumidor final" NAO e atributo do BP e sim de cada transacao.
    // Vem do campo IndFinal do header do documento de marketing (OINV, ODLN,
    // ORIN, ORDR, OPCH, OPOR, ...) e trafega pelo FiscalResolutionRequest.

    public BusinessPartnerAddress? EnderecoEntrega { get; set; }
    public BusinessPartnerAddress? EnderecoCobranca { get; set; }
}

public class BusinessPartnerAddress
{
    public string AddressName { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty; // S = Shipping, B = Billing
    public string Country { get; set; } = "BR";
    public string State { get; set; } = string.Empty; // UF
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
}
