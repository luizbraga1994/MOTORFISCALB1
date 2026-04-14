using System.Data;
using Microsoft.Extensions.Options;
using MOTORFISCALSAPB1.Infrastructure.Options;
using Sap.Data.Hana;

namespace MOTORFISCALSAPB1.Infrastructure.Persistence;

public sealed class HanaConnectionFactory : IHanaConnectionFactory
{
    private readonly HanaOptions _options;

    public HanaConnectionFactory(IOptions<HanaOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Hana:ConnectionString não configurado.");
        }

        if (string.IsNullOrWhiteSpace(_options.Schema))
        {
            throw new InvalidOperationException("Hana:Schema não configurado.");
        }

        QuotedSchema = $"\"{_options.Schema}\"";
    }

    public string QuotedSchema { get; }

    public IDbConnection Create() => new HanaConnection(_options.ConnectionString);
}
