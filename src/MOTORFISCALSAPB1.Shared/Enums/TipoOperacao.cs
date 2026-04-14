namespace MOTORFISCALSAPB1.Shared.Enums;

/// <summary>
/// Tipo de operação fiscal considerada pelo motor.
/// </summary>
public enum TipoOperacao
{
    VendaInterna = 1,
    VendaInterestadual = 2,
    VendaExportacao = 3,
    Devolucao = 4,
    TransferenciaInterna = 5,
    TransferenciaInterestadual = 6,
    Remessa = 7,
    Retorno = 8,
    Bonificacao = 9,
    Brinde = 10,
    Amostra = 11,
    Industrializacao = 12,
    UsoConsumo = 13,
    AtivoImobilizado = 14
}
