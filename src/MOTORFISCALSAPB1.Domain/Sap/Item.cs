namespace MOTORFISCALSAPB1.Domain.Sap;

/// <summary>
/// Projeção de item (OITM) para uso fiscal.
/// </summary>
public class Item
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    /// <summary>
    /// CEST via <c>ONCM.U_TX_CodigoCest</c> (associado ao NCM, não ao item;
    /// join <c>ONCM.AbsEntry = OITM.NCMCode</c>).
    /// </summary>
    public string Cest { get; set; } = string.Empty;
    /// <summary>Origem da mercadoria (0..8) — campo nativo BR <c>OITM.ProductSrc</c>.</summary>
    public int OrigemMercadoria { get; set; }
    public bool InventoryItem { get; set; }
    public bool SalesItem { get; set; }
    public bool PurchaseItem { get; set; }
}
