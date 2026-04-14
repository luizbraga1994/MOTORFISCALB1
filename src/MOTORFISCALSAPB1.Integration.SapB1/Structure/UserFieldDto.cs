using System.Text.Json.Serialization;

namespace MOTORFISCALSAPB1.Integration.SapB1.Structure;

/// <summary>
/// Payload forte para criação de UDF via <c>/UserFieldsMD</c>.
/// </summary>
public sealed class UserFieldDto
{
    /// <summary>
    /// Nome da tabela. Para UDTs, SEM o <c>@</c> (regra do Service Layer);
    /// para tabelas padrão, usa-se o nome (ex: <c>OCRD</c>).
    /// </summary>
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>Nome do campo SEM o prefixo <c>U_</c>.</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>db_Alpha | db_Numeric | db_Date | db_Float | db_Memo</summary>
    [JsonPropertyName("Type")]
    public string Type { get; set; } = "db_Alpha";

    /// <summary>st_None | st_Percentage | st_Price | st_Quantity | st_Rate | st_Sum | st_Time</summary>
    [JsonPropertyName("SubType")]
    public string SubType { get; set; } = "st_None";

    [JsonPropertyName("EditSize")]
    public int EditSize { get; set; } = 50;

    [JsonPropertyName("Mandatory")]
    public string Mandatory { get; set; } = "tNO";

    [JsonPropertyName("DefaultValue")]
    public string? DefaultValue { get; set; }

    [JsonPropertyName("LinkedTable")]
    public string? LinkedTable { get; set; }

    [JsonPropertyName("ValidValuesMD")]
    public List<UserFieldValidValueDto>? ValidValuesMD { get; set; }
}

public sealed class UserFieldValidValueDto
{
    [JsonPropertyName("Value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;
}
