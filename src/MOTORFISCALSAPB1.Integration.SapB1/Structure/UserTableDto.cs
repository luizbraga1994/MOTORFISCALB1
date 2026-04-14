using System.Text.Json.Serialization;

namespace MOTORFISCALSAPB1.Integration.SapB1.Structure;

/// <summary>
/// Payload forte para criação de UDT via Service Layer <c>/UserTablesMD</c>.
/// </summary>
public sealed class UserTableDto
{
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = string.Empty;

    [JsonPropertyName("TableDescription")]
    public string TableDescription { get; set; } = string.Empty;

    /// <summary>bott_NoObject | bott_MasterData | bott_MasterDataLines | bott_Document | bott_DocumentLines</summary>
    [JsonPropertyName("TableType")]
    public string TableType { get; set; } = "bott_NoObject";

    [JsonPropertyName("Archivable")]
    public string Archivable { get; set; } = "tNO";
}
