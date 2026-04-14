using MOTORFISCALSAPB1.Domain.Sap;

namespace MOTORFISCALSAPB1.Application.Abstractions;

/// <summary>
/// Criação/consulta de TaxCodes no SAP B1 via Service Layer.
/// </summary>
public interface ISapTaxCodeService
{
    /// <summary>Verifica se um TaxCode existe no SAP.</summary>
    Task<bool> ExistsAsync(string code, CancellationToken ct);

    /// <summary>Cria um TaxCode no SAP e retorna o código final persistido.</summary>
    Task<string> CreateAsync(TaxCodeDefinition definition, CancellationToken ct);
}
