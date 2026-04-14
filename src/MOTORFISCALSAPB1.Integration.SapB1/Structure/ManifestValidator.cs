using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Integration.SapB1.Structure;

public interface IManifestValidator
{
    ManifestValidationResult Validate(StructureManifest manifest);
}

public sealed class ManifestValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}

public sealed class ManifestValidator : IManifestValidator
{
    private static readonly HashSet<string> ValidTableTypes = new(StringComparer.Ordinal)
    {
        "bott_NoObject", "bott_MasterData", "bott_MasterDataLines", "bott_Document", "bott_DocumentLines"
    };

    private static readonly HashSet<string> ValidFieldTypes = new(StringComparer.Ordinal)
    {
        "db_Alpha", "db_Numeric", "db_Date", "db_Float", "db_Memo"
    };

    private static readonly HashSet<string> ValidSubTypes = new(StringComparer.Ordinal)
    {
        "st_None", "st_Percentage", "st_Price", "st_Quantity", "st_Rate", "st_Sum", "st_Time"
    };

    public ManifestValidationResult Validate(StructureManifest m)
    {
        var r = new ManifestValidationResult();
        if (m is null) { r.Errors.Add("Manifesto nulo."); return r; }

        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in m.UserTables)
        {
            if (string.IsNullOrWhiteSpace(t.Name)) r.Errors.Add("UDT com nome vazio.");
            if (!ValidTableTypes.Contains(t.TableType)) r.Errors.Add($"UDT {t.Name}: TableType inválido '{t.TableType}'.");
            if (!tableNames.Add(t.Name)) r.Errors.Add($"UDT {t.Name}: duplicado.");
        }

        var fieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in m.UserFields)
        {
            if (string.IsNullOrWhiteSpace(f.Table)) r.Errors.Add("UDF sem tabela.");
            if (string.IsNullOrWhiteSpace(f.Name)) r.Errors.Add("UDF sem nome.");
            if (!ValidFieldTypes.Contains(f.FieldType)) r.Errors.Add($"UDF {f.Table}.{f.Name}: Type inválido '{f.FieldType}'.");
            if (!ValidSubTypes.Contains(f.SubType)) r.Errors.Add($"UDF {f.Table}.{f.Name}: SubType inválido '{f.SubType}'.");
            if (f.FieldType == "db_Alpha" && f.Size <= 0) r.Warnings.Add($"UDF {f.Table}.{f.Name}: db_Alpha com Size <= 0.");

            var key = f.Table + "::" + f.Name;
            if (!fieldKeys.Add(key)) r.Errors.Add($"UDF duplicado: {key}.");
        }

        var objectCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in m.UserObjects)
        {
            if (string.IsNullOrWhiteSpace(o.Code)) r.Errors.Add("UDO sem Code.");
            if (string.IsNullOrWhiteSpace(o.TableName)) r.Errors.Add($"UDO {o.Code} sem TableName.");
            if (!objectCodes.Add(o.Code)) r.Errors.Add($"UDO {o.Code}: duplicado.");
        }

        return r;
    }
}
