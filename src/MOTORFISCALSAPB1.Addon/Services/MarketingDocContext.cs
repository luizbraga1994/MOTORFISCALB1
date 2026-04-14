namespace MOTORFISCALSAPB1.Addon.Services
{
    /// <summary>
    /// Contexto mínimo extraído de um documento de marketing SAP (Sales/Purchase).
    /// Usado para montar o request ao motor fiscal sem conter qualquer lógica fiscal.
    /// </summary>
    public sealed class MarketingDocContext
    {
        public string FormUid { get; set; }
        public string FormType { get; set; }
        public int DocumentTypeCode { get; set; } // ObjType 17=ORDR, 13=OINV, 22=OPOR, 18=OPCH, 23=OQUT, etc.

        public string CardCode { get; set; }
        public int BPLId { get; set; }

        public int RowIndex { get; set; }        // Linha da matriz (0-based no addon, 1-based no SAP)
        public string ItemCode { get; set; }
        public string TipoOperacao { get; set; } // Mapeado a partir do tipo de documento

        public string CorrelationId { get; set; }
    }
}
