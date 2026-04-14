using MOTORFISCALSAPB1.Shared.Enums;

namespace MOTORFISCALSAPB1.Addon.Services
{
    /// <summary>
    /// Mapeia FormType do SAP B1 (string) e ObjType para o
    /// <see cref="TipoOperacao"/> usado pelo motor. Sem lógica fiscal —
    /// apenas roteamento de documento → operação candidata.
    /// Os mesmos FormTypes cobrem: Cotação, Pedido, Entrega, Nota de Saída,
    /// Pedido de Compra, Nota de Entrada, Devoluções e Ofertas.
    /// </summary>
    public static class MarketingDocTypeMap
    {
        public static string Resolve(string formType)
        {
            // FormTypes do SAP B1:
            // 149 = Sales Quotation (OQUT)
            // 139 = Sales Order (ORDR)
            // 140 = Delivery  (ODLN)
            // 141 = Return    (ORDN)
            // 133 = A/R Invoice (OINV)
            // 142 = Credit Memo (ORIN)
            // 540 = Purchase Quotation (OPQT)
            // 540000140 = Purchase Request (OPRQ)
            // 142000002 = Purchase Order (OPOR)
            // 143 = Goods Receipt PO (OPDN)
            // 18 (string form: "18") = A/P Invoice (OPCH)
            switch (formType)
            {
                case "149":  // Sales Quotation
                case "139":  // Sales Order
                case "140":  // Delivery
                case "133":  // A/R Invoice
                    return TipoOperacao.VendaInterna.ToString();
                case "141":  // Sales Return
                case "142":  // A/R Credit Memo
                    return TipoOperacao.Devolucao.ToString();
                case "540":
                case "540000140":
                case "142000002":
                case "143":
                case "18":
                    return TipoOperacao.UsoConsumo.ToString();
                default:
                    return TipoOperacao.VendaInterna.ToString();
            }
        }
    }
}
