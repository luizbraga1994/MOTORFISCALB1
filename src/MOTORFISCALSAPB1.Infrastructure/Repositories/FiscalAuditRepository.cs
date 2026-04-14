using Dapper;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Fiscal;
using MOTORFISCALSAPB1.Infrastructure.Persistence;

namespace MOTORFISCALSAPB1.Infrastructure.Repositories;

public sealed class FiscalAuditRepository : IFiscalAuditRepository
{
    private readonly IHanaConnectionFactory _factory;

    public FiscalAuditRepository(IHanaConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(FiscalAuditEntry entry, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO {0}.""@MF_LOG""
  (""Code"", ""Name"",
   ""U_CORRELATIONID"", ""U_USERNAME"", ""U_CARDCODE"", ""U_BPLID"",
   ""U_ITEMCODE"", ""U_RULEID"", ""U_SIGHASH"", ""U_TAXCODE"",
   ""U_CREATED"", ""U_DURATIONMS"", ""U_REQUEST"", ""U_RESPONSE"",
   ""U_ERROR"", ""U_CREATEDAT"")
VALUES
  (:Id, :Id,
   :CorrelationId, :UserName, :CardCode, :BplIdStr,
   :ItemCode, :RuleId, :SigHash, :TaxCode,
   :CreatedFlag, :DurationMs, :Request, :Response,
   :Error, :CreatedAt)";

        using var cn = _factory.Create();
        cn.Open();
        await cn.ExecuteAsync(new CommandDefinition(
            string.Format(sql, _factory.QuotedSchema),
            new
            {
                Id = entry.Id,
                entry.CorrelationId,
                entry.UserName,
                entry.CardCode,
                BplIdStr = entry.BplId?.ToString(),
                entry.ItemCode,
                entry.RuleId,
                SigHash = entry.SignatureHash,
                entry.TaxCode,
                CreatedFlag = entry.TaxCodeCreated ? "Y" : "N",
                entry.DurationMs,
                entry.Request,
                entry.Response,
                entry.Error,
                CreatedAt = entry.CreatedAtUtc
            },
            cancellationToken: ct));
    }
}
