using Dapper;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Fiscal;
using MOTORFISCALSAPB1.Infrastructure.Persistence;

namespace MOTORFISCALSAPB1.Infrastructure.Repositories;

public sealed class TaxCodeMappingRepository : ITaxCodeMappingRepository
{
    private readonly IHanaConnectionFactory _factory;

    public TaxCodeMappingRepository(IHanaConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<TaxCodeMapping?> GetByHashAsync(string signatureHash, CancellationToken ct)
    {
        const string sql = @"
SELECT
  ""Code"" AS ""SignatureHash"",
  ""U_TAXCODE"" AS ""TaxCode"",
  ""U_CANONICAL"" AS ""Canonical"",
  ""U_CREATED_BY"" AS ""CreatedBy"",
  ""U_CREATED_AT"" AS ""CreatedAtUtc""
FROM {0}.""@MF_TAXMAP""
WHERE ""Code"" = :hash";

        using var cn = _factory.Create();
        cn.Open();
        var row = await cn.QueryFirstOrDefaultAsync<Row>(
            new CommandDefinition(string.Format(sql, _factory.QuotedSchema), new { hash = signatureHash }, cancellationToken: ct));
        if (row is null) return null;
        return new TaxCodeMapping(row.SignatureHash, row.TaxCode, row.Canonical ?? string.Empty, row.CreatedBy);
    }

    public async Task AddAsync(TaxCodeMapping mapping, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO {0}.""@MF_TAXMAP""
  (""Code"", ""Name"", ""U_TAXCODE"", ""U_CANONICAL"", ""U_CREATED_BY"", ""U_CREATED_AT"")
VALUES
  (:Id, :Name, :TaxCode, :Canonical, :CreatedBy, :CreatedAt)";

        using var cn = _factory.Create();
        cn.Open();
        await cn.ExecuteAsync(new CommandDefinition(string.Format(sql, _factory.QuotedSchema),
            new
            {
                Id = mapping.Id,
                Name = mapping.TaxCode,
                mapping.TaxCode,
                mapping.Canonical,
                mapping.CreatedBy,
                CreatedAt = mapping.CreatedAtUtc
            },
            cancellationToken: ct));
    }

    private sealed class Row
    {
        public string SignatureHash { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string? Canonical { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
    }
}
