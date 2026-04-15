namespace MOTORFISCALSAPB1.Domain.Fiscal;

/// <summary>
/// Contexto fiscal montado pelo motor ao ler dados reais do SAP
/// (OCRD, CRD1, CRD7, OBPL, OITM).
/// </summary>
public class FiscalResolutionContext
{
    public string CardCode { get; set; } = string.Empty;

    /// <summary>C = Cliente, S = Fornecedor, L = Lead (do OCRD.CardType).</summary>
    public string CardType { get; set; } = string.Empty;

    public string UfDestino { get; set; } = string.Empty;
    public string CidadeDestino { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;
    public string InscricaoEstadual { get; set; } = string.Empty;

    public bool ContribuinteICMS { get; set; }
    public bool ConsumidorFinal { get; set; }

    public int BplId { get; set; }
    public string UfOrigem { get; set; } = string.Empty;

    public string ItemCode { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string Cest { get; set; } = string.Empty;
    public int OrigemMercadoria { get; set; }

    public string TipoOperacao { get; set; } = string.Empty;

    /// <summary>Regime tributário da filial emissora.</summary>
    public int RegimeTributarioFilial { get; set; }

    /// <summary>CorrelationId para rastreabilidade fim-a-fim.</summary>
    public string? CorrelationId { get; set; }

    public bool IsOperacaoInterestadual()
    {
        if (string.IsNullOrWhiteSpace(UfOrigem) || string.IsNullOrWhiteSpace(UfDestino))
        {
            return false;
        }

        return !string.Equals(UfOrigem, UfDestino, StringComparison.OrdinalIgnoreCase);
    }
}
