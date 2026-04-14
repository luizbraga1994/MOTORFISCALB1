namespace MOTORFISCALSAPB1.Infrastructure.Options;

/// <summary>
/// Configuracao de conexao com SAP HANA. Alinhada com o padrao do
/// PortalSapB1 — secao <c>HanaDbConnection</c> no appsettings.
/// </summary>
public sealed class HanaOptions
{
    public const string SectionName = "HanaDbConnection";

    /// <summary>Host (pode conter porta, ex.: <c>saphaalbieri:30015</c>).</summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>Porta (default HANA index 00 = 30015).</summary>
    public string Port { get; set; } = "30015";

    /// <summary>Company DB / Schema (ex.: <c>SBODEMOBR</c>).</summary>
    public string Database { get; set; } = string.Empty;

    public string UserID { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; } = 10;

    /// <summary>Timeout de abertura de conexao (segundos).</summary>
    public int ConnectionTimeout { get; set; } = 60;

    /// <summary>Timeout default para comandos Dapper (segundos).</summary>
    public int CommandTimeout { get; set; } = 300;

    /// <summary>
    /// Monta a connection string ADO.NET para o HANA (driver
    /// Sap.Data.Hana.Core.v2.1), com pooling habilitado.
    /// </summary>
    public string BuildConnectionString()
    {
        return $"Server={Server};UserID={UserID};Password={Password};CS={Database}" +
               $";Pooling=true;MaxPoolSize={MaxPoolSize};MinPoolSize={MinPoolSize}" +
               $";Connection Timeout={ConnectionTimeout};CommandTimeout={CommandTimeout}";
    }
}
