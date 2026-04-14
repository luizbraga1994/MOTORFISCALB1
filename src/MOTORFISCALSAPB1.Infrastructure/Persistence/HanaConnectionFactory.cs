using System.Data;
using Microsoft.Extensions.Options;
using MOTORFISCALSAPB1.Infrastructure.Options;
using Sap.Data.Hana;

namespace MOTORFISCALSAPB1.Infrastructure.Persistence;

public sealed class HanaConnectionFactory : IHanaConnectionFactory
{
    private readonly HanaOptions _options;
    private readonly string _connectionString;

    public HanaConnectionFactory(IOptions<HanaOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.Server))
            throw new InvalidOperationException("HanaDbConnection:Server nao configurado.");
        if (string.IsNullOrWhiteSpace(_options.Database))
            throw new InvalidOperationException("HanaDbConnection:Database nao configurado.");
        if (string.IsNullOrWhiteSpace(_options.UserID))
            throw new InvalidOperationException("HanaDbConnection:UserID nao configurado.");

        _connectionString = _options.BuildConnectionString();
        QuotedSchema = $"\"{_options.Database}\"";
    }

    /// <summary>Schema da company, ja entre aspas, para uso em SQL.</summary>
    public string QuotedSchema { get; }

    /// <summary>Command timeout (segundos) a ser aplicado por Dapper.</summary>
    public int CommandTimeoutSeconds => _options.CommandTimeout;

    public IDbConnection Create() => new HanaConnection(_connectionString);
}
