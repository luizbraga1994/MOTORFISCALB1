using System.Data;
using System.Text.Json;
using Dapper;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Fiscal;
using MOTORFISCALSAPB1.Infrastructure.Persistence;

namespace MOTORFISCALSAPB1.Infrastructure.Repositories;

/// <summary>
/// Repositório de regras fiscais persistidas na UDT <c>@MF_RULE</c> e
/// <c>@MF_RULE_RES</c> (resultado serializado em JSON em um campo memo).
/// </summary>
public sealed class FiscalRuleRepository : IFiscalRuleRepository
{
    private readonly IHanaConnectionFactory _factory;

    public FiscalRuleRepository(IHanaConnectionFactory factory)
    {
        _factory = factory;
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<FiscalRule>> GetAllActiveAsync(CancellationToken ct)
    {
        const string sql = @"
SELECT
  ""Code"" AS ""Id"",
  ""Name"" AS ""Descricao"",
  TO_INT(""U_PRIORITY"")   AS ""Prioridade"",
  ""U_ACTIVE""             AS ""AtivoFlag"",
  ""U_CARDCODE""           AS ""CardCode"",
  ""U_ITEMCODE""           AS ""ItemCode"",
  ""U_BPLID""              AS ""BplIdStr"",
  ""U_NCM""                AS ""Ncm"",
  ""U_TIPO_OP""            AS ""TipoOperacao"",
  ""U_UF_ORIGEM""          AS ""UfOrigem"",
  ""U_UF_DESTINO""         AS ""UfDestino"",
  ""U_CONTRIB_ICMS""       AS ""ContribIcmsFlag"",
  ""U_CONSUMIDOR_FINAL""   AS ""ConsFinalFlag"",
  ""U_REGIME""             AS ""RegimeStr"",
  ""U_VIG_INICIO""         AS ""VigInicio"",
  ""U_VIG_FIM""            AS ""VigFim"",
  ""U_RESULT_JSON""        AS ""ResultJson""
FROM {0}.""@MF_RULE""
WHERE ""U_ACTIVE"" = 'Y'";

        using var cn = _factory.Create();
        cn.Open();
        var rows = (await cn.QueryAsync<RuleRow>(
            new CommandDefinition(string.Format(sql, _factory.QuotedSchema), cancellationToken: ct))).ToList();

        return rows.Select(Map).ToList();
    }

    public async Task<FiscalRule?> GetByIdAsync(string id, CancellationToken ct)
    {
        const string sql = @"
SELECT
  ""Code"" AS ""Id"", ""Name"" AS ""Descricao"",
  TO_INT(""U_PRIORITY"") AS ""Prioridade"",
  ""U_ACTIVE"" AS ""AtivoFlag"",
  ""U_CARDCODE"" AS ""CardCode"", ""U_ITEMCODE"" AS ""ItemCode"",
  ""U_BPLID"" AS ""BplIdStr"", ""U_NCM"" AS ""Ncm"",
  ""U_TIPO_OP"" AS ""TipoOperacao"",
  ""U_UF_ORIGEM"" AS ""UfOrigem"", ""U_UF_DESTINO"" AS ""UfDestino"",
  ""U_CONTRIB_ICMS"" AS ""ContribIcmsFlag"",
  ""U_CONSUMIDOR_FINAL"" AS ""ConsFinalFlag"",
  ""U_REGIME"" AS ""RegimeStr"",
  ""U_VIG_INICIO"" AS ""VigInicio"",
  ""U_VIG_FIM"" AS ""VigFim"",
  ""U_RESULT_JSON"" AS ""ResultJson""
FROM {0}.""@MF_RULE""
WHERE ""Code"" = :id";

        using var cn = _factory.Create();
        cn.Open();
        var row = await cn.QueryFirstOrDefaultAsync<RuleRow>(
            new CommandDefinition(string.Format(sql, _factory.QuotedSchema), new { id }, cancellationToken: ct));
        return row is null ? null : Map(row);
    }

    public async Task UpsertAsync(FiscalRule rule, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rule);
        const string sql = @"
UPSERT {0}.""@MF_RULE""
 ( ""Code"", ""Name"", ""U_PRIORITY"", ""U_ACTIVE"",
   ""U_CARDCODE"", ""U_ITEMCODE"", ""U_BPLID"", ""U_NCM"",
   ""U_TIPO_OP"", ""U_UF_ORIGEM"", ""U_UF_DESTINO"",
   ""U_CONTRIB_ICMS"", ""U_CONSUMIDOR_FINAL"", ""U_REGIME"",
   ""U_VIG_INICIO"", ""U_VIG_FIM"", ""U_RESULT_JSON"" )
VALUES
 ( :Id, :Descricao, :Prioridade, :AtivoFlag,
   :CardCode, :ItemCode, :BplIdStr, :Ncm,
   :TipoOperacao, :UfOrigem, :UfDestino,
   :ContribIcmsFlag, :ConsFinalFlag, :RegimeStr,
   :VigInicio, :VigFim, :ResultJson )
WITH PRIMARY KEY";

        using var cn = _factory.Create();
        cn.Open();

        var p = new DynamicParameters();
        p.Add("Id", rule.Id);
        p.Add("Descricao", rule.Descricao);
        p.Add("Prioridade", rule.Prioridade);
        var isActive = rule.EstaVigente(DateTime.UtcNow) ? "Y" : "N";
        p.Add("AtivoFlag", isActive);
        p.Add("CardCode", rule.CardCode);
        p.Add("ItemCode", rule.ItemCode);
        p.Add("BplIdStr", rule.BplId?.ToString());
        p.Add("Ncm", rule.Ncm);
        p.Add("TipoOperacao", rule.TipoOperacao);
        p.Add("UfOrigem", rule.UfOrigem);
        p.Add("UfDestino", rule.UfDestino);
        p.Add("ContribIcmsFlag", rule.ContribuinteIcms.HasValue ? (rule.ContribuinteIcms.Value ? "Y" : "N") : null);
        p.Add("ConsFinalFlag", rule.ConsumidorFinal.HasValue ? (rule.ConsumidorFinal.Value ? "Y" : "N") : null);
        p.Add("RegimeStr", rule.RegimeTributarioFilial?.ToString());
        p.Add("VigInicio", rule.VigenciaInicio, DbType.DateTime);
        p.Add("VigFim", rule.VigenciaFim, DbType.DateTime);
        p.Add("ResultJson", JsonSerializer.Serialize(rule.Resultado, JsonOpts));

        await cn.ExecuteAsync(new CommandDefinition(string.Format(sql, _factory.QuotedSchema), p, cancellationToken: ct));
    }

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        const string sql = @"DELETE FROM {0}.""@MF_RULE"" WHERE ""Code"" = :id";
        using var cn = _factory.Create();
        cn.Open();
        await cn.ExecuteAsync(new CommandDefinition(string.Format(sql, _factory.QuotedSchema), new { id }, cancellationToken: ct));
    }

    private static FiscalRule Map(RuleRow row)
    {
        var result = string.IsNullOrWhiteSpace(row.ResultJson)
            ? new FiscalRuleResult()
            : JsonSerializer.Deserialize<FiscalRuleResult>(row.ResultJson, JsonOpts) ?? new FiscalRuleResult();

        var rule = new FiscalRule(row.Id, row.Descricao ?? string.Empty, row.Prioridade, result, row.VigInicio ?? DateTime.MinValue);
        rule.DefinirEscopo(
            row.CardCode,
            row.ItemCode,
            int.TryParse(row.BplIdStr, out var bpl) ? bpl : null,
            row.Ncm,
            row.TipoOperacao,
            row.UfOrigem,
            row.UfDestino,
            ParseFlag(row.ContribIcmsFlag),
            ParseFlag(row.ConsFinalFlag),
            int.TryParse(row.RegimeStr, out var regime) ? regime : null);

        if (!string.Equals(row.AtivoFlag, "Y", StringComparison.OrdinalIgnoreCase))
        {
            rule.Desativar();
        }
        return rule;
    }

    private static bool? ParseFlag(string? flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return null;
        return string.Equals(flag, "Y", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RuleRow
    {
        public string Id { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int Prioridade { get; set; }
        public string? AtivoFlag { get; set; }
        public string? CardCode { get; set; }
        public string? ItemCode { get; set; }
        public string? BplIdStr { get; set; }
        public string? Ncm { get; set; }
        public string? TipoOperacao { get; set; }
        public string? UfOrigem { get; set; }
        public string? UfDestino { get; set; }
        public string? ContribIcmsFlag { get; set; }
        public string? ConsFinalFlag { get; set; }
        public string? RegimeStr { get; set; }
        public DateTime? VigInicio { get; set; }
        public DateTime? VigFim { get; set; }
        public string? ResultJson { get; set; }
    }
}
