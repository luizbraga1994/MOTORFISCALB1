namespace MOTORFISCALSAPB1.Shared.Helpers;

/// <summary>
/// Convenções SAP B1 para normalização de nomes de tabelas e campos de usuário.
/// </summary>
/// <remarks>
/// UDTs: TableID com <c>@</c> (ex: <c>@MF_RULE</c>).<br/>
/// Tabelas padrão (OCRD, OITM, OBPL, ...): TableID sem <c>@</c>.<br/>
/// UDFs: AliasID sempre SEM o prefixo <c>U_</c>. A coluna física gerada pelo SAP recebe automaticamente <c>U_</c>.
/// </remarks>
public static class SapNamingConventions
{
    public const string UserTablePrefix = "@";
    public const string UserFieldPrefix = "U_";

    /// <summary>
    /// Retorna <c>true</c> se a tabela é uma tabela padrão do SAP (ex: OCRD, OITM).
    /// </summary>
    public static bool IsStandardTable(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return false;
        }

        return !tableName.TrimStart().StartsWith(UserTablePrefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Normaliza o TableID para ser usado na consulta ao CUFD.
    /// UDTs recebem o <c>@</c>, tabelas padrão não.
    /// </summary>
    public static string NormalizeTableId(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Nome de tabela não pode ser vazio.", nameof(tableName));
        }

        var trimmed = tableName.Trim();
        if (IsStandardTable(trimmed))
        {
            return trimmed.ToUpperInvariant();
        }

        return trimmed.StartsWith(UserTablePrefix, StringComparison.Ordinal)
            ? trimmed
            : UserTablePrefix + trimmed;
    }

    /// <summary>
    /// Retorna o nome da UDT sem o <c>@</c> (usado em UserTablesMD e em metadados).
    /// </summary>
    public static string StripUserTablePrefix(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Nome de tabela não pode ser vazio.", nameof(tableName));
        }

        var trimmed = tableName.Trim();
        return trimmed.StartsWith(UserTablePrefix, StringComparison.Ordinal)
            ? trimmed.Substring(1)
            : trimmed;
    }

    /// <summary>
    /// Normaliza o AliasID: SEMPRE sem o prefixo <c>U_</c>.
    /// </summary>
    public static string NormalizeFieldAlias(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Nome de campo não pode ser vazio.", nameof(fieldName));
        }

        var trimmed = fieldName.Trim();
        return trimmed.StartsWith(UserFieldPrefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed.Substring(UserFieldPrefix.Length)
            : trimmed;
    }

    /// <summary>
    /// Retorna o nome físico da coluna (com <c>U_</c>) para uso em SQL.
    /// </summary>
    public static string PhysicalColumnName(string fieldName)
    {
        var alias = NormalizeFieldAlias(fieldName);
        return UserFieldPrefix + alias;
    }
}
