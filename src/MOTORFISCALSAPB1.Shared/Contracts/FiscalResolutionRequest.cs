using MOTORFISCALSAPB1.Shared.Enums;

namespace MOTORFISCALSAPB1.Shared.Contracts;

/// <summary>
/// Requisição de resolução fiscal recebida pela API.
/// O motor monta o contexto a partir desse payload lendo o SAP (OCRD, CRD1, CRD7, OBPL, OITM).
/// </summary>
public class FiscalResolutionRequest
{
    public string CardCode { get; set; } = string.Empty;
    public int BplId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public TipoOperacao TipoOperacao { get; set; }
    public decimal? Quantidade { get; set; }
    public decimal? ValorUnitario { get; set; }
    public string? Observacao { get; set; }
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Indicador de consumidor final. Vem do campo <c>IndFinal</c> do header
    /// do documento de marketing (OINV, ODLN, ORIN, ORDR, OPCH, OPOR, ...)
    /// na localizacao BR do SAP B1. Eh atributo POR TRANSACAO, nao por BP.
    /// </summary>
    public bool ConsumidorFinal { get; set; }
}
