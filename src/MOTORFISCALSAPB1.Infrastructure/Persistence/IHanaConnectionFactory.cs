using System.Data;

namespace MOTORFISCALSAPB1.Infrastructure.Persistence;

public interface IHanaConnectionFactory
{
    IDbConnection Create();
    /// <summary>Schema (quoted) a ser usado como prefixo em SQL HANA, ex: <c>"SBO_COMP"</c>.</summary>
    string QuotedSchema { get; }
}
