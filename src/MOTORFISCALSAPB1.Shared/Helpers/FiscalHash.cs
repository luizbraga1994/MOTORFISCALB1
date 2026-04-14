using System.Security.Cryptography;
using System.Text;

namespace MOTORFISCALSAPB1.Shared.Helpers;

/// <summary>
/// Gera hashes determinísticos para assinaturas fiscais.
/// </summary>
public static class FiscalHash
{
    /// <summary>
    /// Calcula um SHA-256 determinístico e retorna em hex minúsculo.
    /// </summary>
    public static string Sha256(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
