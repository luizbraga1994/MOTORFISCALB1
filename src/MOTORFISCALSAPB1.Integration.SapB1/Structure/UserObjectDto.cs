using System.Text.Json.Serialization;

namespace MOTORFISCALSAPB1.Integration.SapB1.Structure;

/// <summary>
/// Payload forte para criação de UDO via <c>/UserObjectsMD</c>.
/// </summary>
public sealed class UserObjectDto
{
    [JsonPropertyName("Code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>boud_MasterData | boud_Document</summary>
    [JsonPropertyName("ObjectType")]
    public string ObjectType { get; set; } = "boud_MasterData";

    [JsonPropertyName("CanCancel")]
    public string CanCancel { get; set; } = "tYES";
    [JsonPropertyName("CanDelete")]
    public string CanDelete { get; set; } = "tYES";
    [JsonPropertyName("CanLog")]
    public string CanLog { get; set; } = "tYES";
    [JsonPropertyName("CanFind")]
    public string CanFind { get; set; } = "tYES";
    [JsonPropertyName("ManageSeries")]
    public string ManageSeries { get; set; } = "tYES";

    [JsonPropertyName("UseUniqueFormType")]
    public string UseUniqueFormType { get; set; } = "tNO";

    [JsonPropertyName("UserObjectMD_ChildTables")]
    public List<UserObjectChildTableDto>? ChildTables { get; set; }

    [JsonPropertyName("UserObjectMD_FindColumns")]
    public List<UserObjectFindColumnDto>? FindColumns { get; set; }
}

public sealed class UserObjectChildTableDto
{
    [JsonPropertyName("SonNumber")]
    public int SonNumber { get; set; }
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = string.Empty;
    [JsonPropertyName("ObjectName")]
    public string? ObjectName { get; set; }
}

public sealed class UserObjectFindColumnDto
{
    [JsonPropertyName("ColumnAlias")]
    public string ColumnAlias { get; set; } = string.Empty;
    [JsonPropertyName("ColumnDescription")]
    public string ColumnDescription { get; set; } = string.Empty;
}
