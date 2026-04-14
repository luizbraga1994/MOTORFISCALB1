using MOTORFISCALSAPB1.Domain.Sap;

namespace MOTORFISCALSAPB1.Application.Abstractions;

/// <summary>
/// Porta de leitura dos dados reais do SAP Business One (via HANA).
/// </summary>
public interface ISapReadPort
{
    Task<BusinessPartner?> GetBusinessPartnerAsync(string cardCode, CancellationToken ct);
    Task<Branch?> GetBranchAsync(int bplId, CancellationToken ct);
    Task<Item?> GetItemAsync(string itemCode, CancellationToken ct);
    Task<bool> UserFieldExistsAsync(string tableId, string aliasId, CancellationToken ct);
}
