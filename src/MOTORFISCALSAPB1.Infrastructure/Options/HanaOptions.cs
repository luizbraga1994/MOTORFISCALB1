namespace MOTORFISCALSAPB1.Infrastructure.Options;

public sealed class HanaOptions
{
    public const string SectionName = "Hana";

    /// <summary>Schema da company SAP no HANA (ex: <c>SBO_COMP</c>).</summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>Connection string HANA.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 30;
}
