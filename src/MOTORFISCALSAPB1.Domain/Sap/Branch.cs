namespace MOTORFISCALSAPB1.Domain.Sap;

/// <summary>
/// Projeção de filial (OBPL).
/// </summary>
public class Branch
{
    public int BplId { get; set; }
    public string BplName { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string InscricaoEstadual { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    /// <summary>UDF <c>U_MF_REGIME</c> na OBPL.</summary>
    public int RegimeTributario { get; set; }
    /// <summary>UDF <c>U_MF_ATIVIDADE</c> na OBPL.</summary>
    public string? Atividade { get; set; }
}
