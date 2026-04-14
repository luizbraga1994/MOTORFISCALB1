using System.Text.Json.Serialization;

namespace MOTORFISCALSAPB1.Shared.Contracts;

/// <summary>
/// Manifesto forte para criação de estrutura SAP via Service Layer.
/// Carregado a partir do <c>structure.json</c>.
/// </summary>
public class StructureManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("userTables")]
    public List<UserTableDefinition> UserTables { get; set; } = new();

    [JsonPropertyName("userFields")]
    public List<UserFieldDefinition> UserFields { get; set; } = new();

    [JsonPropertyName("userObjects")]
    public List<UserObjectDefinition> UserObjects { get; set; } = new();
}

public class UserTableDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// BoUTBTableType: <c>bott_NoObject</c>, <c>bott_MasterData</c>,
    /// <c>bott_MasterDataLines</c>, <c>bott_Document</c>, <c>bott_DocumentLines</c>.
    /// </summary>
    [JsonPropertyName("tableType")]
    public string TableType { get; set; } = "bott_NoObject";

    [JsonPropertyName("archivable")]
    public bool Archivable { get; set; }
}

public class UserFieldDefinition
{
    /// <summary>
    /// Nome da tabela. Para UDT pode ser <c>@MF_RULE</c> ou <c>MF_RULE</c>;
    /// a camada de normalização corrige.
    /// </summary>
    [JsonPropertyName("table")]
    public string Table { get; set; } = string.Empty;

    /// <summary>
    /// Nome do campo. Pode vir com ou sem o prefixo <c>U_</c>.
    /// A camada de normalização produz o AliasID correto (sem <c>U_</c>).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// BoFieldTypes: <c>db_Alpha</c>, <c>db_Numeric</c>, <c>db_Date</c>,
    /// <c>db_Float</c>, <c>db_Memo</c>.
    /// </summary>
    [JsonPropertyName("fieldType")]
    public string FieldType { get; set; } = "db_Alpha";

    /// <summary>
    /// BoFldSubTypes: <c>st_None</c>, <c>st_Percentage</c>, <c>st_Price</c>,
    /// <c>st_Quantity</c>, <c>st_Rate</c>, <c>st_Sum</c>, <c>st_Time</c>.
    /// </summary>
    [JsonPropertyName("subType")]
    public string SubType { get; set; } = "st_None";

    [JsonPropertyName("size")]
    public int Size { get; set; } = 50;

    [JsonPropertyName("mandatory")]
    public bool Mandatory { get; set; }

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }

    [JsonPropertyName("validValues")]
    public List<ValidValueDefinition>? ValidValues { get; set; }

    [JsonPropertyName("linkedTable")]
    public string? LinkedTable { get; set; }
}

public class ValidValueDefinition
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class UserObjectDefinition
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>boud_MasterData / boud_Document / boud_NoList (no contexto SAP B1).</summary>
    [JsonPropertyName("objectType")]
    public string ObjectType { get; set; } = "boud_MasterData";

    [JsonPropertyName("canCancel")]
    public bool CanCancel { get; set; } = true;

    [JsonPropertyName("canDelete")]
    public bool CanDelete { get; set; } = true;

    [JsonPropertyName("canLog")]
    public bool CanLog { get; set; } = true;

    [JsonPropertyName("canFind")]
    public bool CanFind { get; set; } = true;

    [JsonPropertyName("manageSeries")]
    public bool ManageSeries { get; set; } = true;

    [JsonPropertyName("childTables")]
    public List<string>? ChildTables { get; set; }

    [JsonPropertyName("findColumns")]
    public List<UserObjectFindColumn>? FindColumns { get; set; }
}

public class UserObjectFindColumn
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
