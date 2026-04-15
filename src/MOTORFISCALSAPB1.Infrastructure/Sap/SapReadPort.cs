using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Sap;
using MOTORFISCALSAPB1.Infrastructure.Persistence;
using MOTORFISCALSAPB1.Shared.Helpers;

namespace MOTORFISCALSAPB1.Infrastructure.Sap;

/// <summary>
/// Leitura de dados reais do SAP via HANA (OCRD, CRD1, CRD7, OBPL, OITM, CUFD).
/// Todas as queries usam sintaxe HANA e o schema configurado.
/// </summary>
public sealed class SapReadPort : ISapReadPort
{
    private readonly IHanaConnectionFactory _factory;
    private readonly ILogger<SapReadPort> _logger;

    public SapReadPort(IHanaConnectionFactory factory, ILogger<SapReadPort> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<BusinessPartner?> GetBusinessPartnerAsync(string cardCode, CancellationToken ct)
    {
        // NOTA: "ConsumidorFinal" NAO e lido daqui. Eh atributo do header do
        // documento de marketing (OINV/ODLN/etc.) via campo nativo IndFinal.
        // Viaja pelo FiscalResolutionRequest.ConsumidorFinal e nao pelo BP.
        const string sqlHeader = @"
SELECT
  ""CardCode"", ""CardName"", ""CardType"", ""LicTradNum""
FROM {0}.""OCRD""
WHERE ""CardCode"" = :cardCode";

        const string sqlAddress = @"
SELECT
  ""Address"" AS ""AddressName"",
  ""AdresType"" AS ""AddressType"",
  COALESCE(""Country"", 'BR') AS ""Country"",
  COALESCE(""State"", '') AS ""State"",
  COALESCE(""City"",  '') AS ""City"",
  COALESCE(""ZipCode"", '') AS ""ZipCode"",
  COALESCE(""Street"", '') AS ""Street"",
  COALESCE(""StreetNo"", '') AS ""Number"",
  COALESCE(""Block"", '') AS ""District""
FROM {0}.""CRD1""
WHERE ""CardCode"" = :cardCode";

        const string sqlFiscal = @"
SELECT TOP 1
  COALESCE(""TaxId1"", '') AS ""InscricaoEstadual"",
  COALESCE(""TaxIdIdent"", '') AS ""TaxIdIdent""
FROM {0}.""CRD7""
WHERE ""CardCode"" = :cardCode
ORDER BY ""Address"" ASC";

        using var cn = _factory.Create();
        cn.Open();
        var schema = _factory.QuotedSchema;

        var header = await QueryFirstOrDefaultAsync<BpHeaderRow>(cn, string.Format(sqlHeader, schema), new { cardCode }, ct);
        if (header is null) return null;

        var addresses = (await QueryAsync<AddressRow>(cn, string.Format(sqlAddress, schema), new { cardCode }, ct)).ToList();
        var fiscal = await QueryFirstOrDefaultAsync<FiscalRow>(cn, string.Format(sqlFiscal, schema), new { cardCode }, ct);

        var bp = new BusinessPartner
        {
            CardCode = header.CardCode,
            CardName = header.CardName ?? string.Empty,
            CardType = header.CardType ?? string.Empty,
            LicTradNum = header.LicTradNum ?? string.Empty,
            InscricaoEstadual = fiscal?.InscricaoEstadual ?? string.Empty,
            ContribuinteIcms = !string.IsNullOrWhiteSpace(fiscal?.InscricaoEstadual) &&
                               !string.Equals(fiscal.InscricaoEstadual, "ISENTO", StringComparison.OrdinalIgnoreCase),
            EnderecoEntrega = MapAddress(addresses.FirstOrDefault(a => a.AddressType == "S")),
            EnderecoCobranca = MapAddress(addresses.FirstOrDefault(a => a.AddressType == "B"))
        };

        return bp;
    }

    public async Task<Branch?> GetBranchAsync(int bplId, CancellationToken ct)
    {
        // Campos nativos BR: ProfFax = regime tributario (perfil fiscal).
        // MF_ATIVIDADE (CNAE) permanece como UDF custom.
        const string sql = @"
SELECT
  ""BPLId"", ""BPLName"", ""TaxIdNum"" AS ""Cnpj"",
  COALESCE(""AddrType"", '') AS ""AddrType"",
  COALESCE(""State"", '') AS ""Uf"",
  COALESCE(""City"", '') AS ""Cidade"",
  COALESCE(""ProfFax"", '0') AS ""RegimeTributario"",
  ""U_MF_ATIVIDADE"" AS ""Atividade""
FROM {0}.""OBPL""
WHERE ""BPLId"" = :bplId";

        using var cn = _factory.Create();
        cn.Open();
        var row = await QueryFirstOrDefaultAsync<BranchRow>(cn, string.Format(sql, _factory.QuotedSchema), new { bplId }, ct);
        if (row is null) return null;

        return new Branch
        {
            BplId = row.BPLId,
            BplName = row.BPLName ?? string.Empty,
            Cnpj = row.Cnpj ?? string.Empty,
            Uf = row.Uf ?? string.Empty,
            Cidade = row.Cidade ?? string.Empty,
            RegimeTributario = int.TryParse(row.RegimeTributario, out var rt) ? rt : 0,
            Atividade = row.Atividade
        };
    }

    public async Task<Item?> GetItemAsync(string itemCode, CancellationToken ct)
    {
        // Campos nativos BR:
        //   OITM.ProductSrc       -> origem da mercadoria (0-8).
        //   ONCM.""Code""         -> NCM (join por OITM.NCMCode = ONCM.AbsEntry).
        //   ONCM.""U_TX_CodigoCest""-> CEST (associado ao NCM, nao ao item).
        const string sql = @"
SELECT
  i.""ItemCode"",
  i.""ItemName"",
  COALESCE(i.""NCMCode"", 0)       AS ""NcmCode"",
  COALESCE(n.""Code"", '')         AS ""Ncm"",
  COALESCE(n.""U_TX_CodigoCest"", '') AS ""Cest"",
  COALESCE(i.""ProductSrc"", '0')  AS ""Origem"",
  i.""InvntItem"", i.""SellItem"", i.""PrchseItem""
FROM {0}.""OITM"" i
LEFT JOIN {0}.""ONCM"" n ON n.""AbsEntry"" = i.""NCMCode""
WHERE i.""ItemCode"" = :itemCode";

        using var cn = _factory.Create();
        cn.Open();
        var row = await QueryFirstOrDefaultAsync<ItemRow>(cn, string.Format(sql, _factory.QuotedSchema), new { itemCode }, ct);
        if (row is null) return null;

        return new Item
        {
            ItemCode = row.ItemCode,
            ItemName = row.ItemName ?? string.Empty,
            Ncm = row.Ncm ?? string.Empty,
            Cest = row.Cest ?? string.Empty,
            OrigemMercadoria = int.TryParse(row.Origem, out var o) ? o : 0,
            InventoryItem = string.Equals(row.InvntItem, "Y", StringComparison.OrdinalIgnoreCase),
            SalesItem = string.Equals(row.SellItem, "Y", StringComparison.OrdinalIgnoreCase),
            PurchaseItem = string.Equals(row.PrchseItem, "Y", StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <inheritdoc />
    public async Task<bool> UserFieldExistsAsync(string tableId, string aliasId, CancellationToken ct)
    {
        // Normaliza conforme convenção SAP: UDTs com '@', padrão sem; AliasID sem 'U_'.
        var table = SapNamingConventions.IsStandardTable(tableId)
            ? SapNamingConventions.NormalizeTableId(tableId).TrimStart('@')
            : SapNamingConventions.NormalizeTableId(tableId); // mantém '@'
        var alias = SapNamingConventions.NormalizeFieldAlias(aliasId);

        const string sql = @"
SELECT COUNT(1)
FROM {0}.""CUFD""
WHERE ""TableID"" = :tableId AND ""AliasID"" = :aliasId";

        using var cn = _factory.Create();
        cn.Open();
        var count = await QueryFirstOrDefaultAsync<long>(
            cn,
            string.Format(sql, _factory.QuotedSchema),
            new { tableId = table, aliasId = alias },
            ct);
        return count > 0;
    }

    private static BusinessPartnerAddress? MapAddress(AddressRow? row)
    {
        if (row is null) return null;
        return new BusinessPartnerAddress
        {
            AddressName = row.AddressName,
            AddressType = row.AddressType,
            Country = row.Country,
            State = row.State,
            City = row.City,
            ZipCode = row.ZipCode,
            Street = row.Street,
            Number = row.Number,
            District = row.District
        };
    }

    private static Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection cn, string sql, object param, CancellationToken ct)
        => cn.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct));

    private static Task<IEnumerable<T>> QueryAsync<T>(IDbConnection cn, string sql, object param, CancellationToken ct)
        => cn.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct));

    private sealed class BpHeaderRow
    {
        public string CardCode { get; set; } = string.Empty;
        public string? CardName { get; set; }
        public string? CardType { get; set; }
        public string? LicTradNum { get; set; }
    }

    private sealed class AddressRow
    {
        public string AddressName { get; set; } = string.Empty;
        public string AddressType { get; set; } = string.Empty;
        public string Country { get; set; } = "BR";
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
    }

    private sealed class FiscalRow
    {
        public string? InscricaoEstadual { get; set; }
        public string? TaxIdIdent { get; set; }
    }

    private sealed class BranchRow
    {
        public int BPLId { get; set; }
        public string? BPLName { get; set; }
        public string? Cnpj { get; set; }
        public string? AddrType { get; set; }
        public string? Uf { get; set; }
        public string? Cidade { get; set; }
        public string? RegimeTributario { get; set; }
        public string? Atividade { get; set; }
    }

    private sealed class ItemRow
    {
        public string ItemCode { get; set; } = string.Empty;
        public string? ItemName { get; set; }
        public int NcmCode { get; set; }
        public string? Ncm { get; set; }
        public string? Cest { get; set; }
        public string? Origem { get; set; }
        public string? InvntItem { get; set; }
        public string? SellItem { get; set; }
        public string? PrchseItem { get; set; }
    }
}
