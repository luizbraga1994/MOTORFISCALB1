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
    /// <summary>Campo nativo BR <c>OBPL.ProfFax</c> (perfil/regime fiscal).</summary>
    public int RegimeTributario { get; set; }
}
