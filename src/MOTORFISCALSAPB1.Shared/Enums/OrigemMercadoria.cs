namespace MOTORFISCALSAPB1.Shared.Enums;

/// <summary>
/// Origem da mercadoria conforme tabela A do CST ICMS.
/// </summary>
public enum OrigemMercadoria
{
    Nacional = 0,
    EstrangeiraImportacaoDireta = 1,
    EstrangeiraAdquiridaMercadoInterno = 2,
    NacionalConteudoImportacaoSuperior40 = 3,
    NacionalProcessoProdutivoBasico = 4,
    NacionalConteudoImportacaoInferiorOuIgual40 = 5,
    EstrangeiraImportacaoDiretaSemSimilar = 6,
    EstrangeiraAdquiridaMercadoInternoSemSimilar = 7,
    NacionalConteudoImportacaoSuperior70 = 8
}
