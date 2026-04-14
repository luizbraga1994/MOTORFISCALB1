using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Sap;
using MOTORFISCALSAPB1.Integration.SapB1.ServiceLayer;

namespace MOTORFISCALSAPB1.Integration.SapB1.TaxCodes;

/// <summary>
/// Implementa <see cref="ISapTaxCodeService"/> via Service Layer.
/// Usa o endpoint <c>/SalesTaxCodes</c> (e <c>/PurchaseTaxCodes</c> quando apropriado).
/// Em instalações brasileiras costuma haver também <c>/SalesTaxCodesBrazil</c> /
/// <c>/PurchaseTaxCodesBrazil</c>; o <see cref="TaxCodeDefinition.Category"/> 'O' vai
/// para vendas, 'I' para compras.
/// </summary>
public sealed class SapTaxCodeService : ISapTaxCodeService
{
    private readonly IServiceLayerClient _client;
    private readonly ILogger<SapTaxCodeService> _logger;

    public SapTaxCodeService(IServiceLayerClient client, ILogger<SapTaxCodeService> logger)
    {
        _client = client;
        _logger = logger;
    }

    private static string EndpointFor(string category) =>
        string.Equals(category, "I", StringComparison.OrdinalIgnoreCase) ? "PurchaseTaxCodes" : "SalesTaxCodes";

    public async Task<bool> ExistsAsync(string code, CancellationToken ct)
    {
        foreach (var endpoint in new[] { "SalesTaxCodes", "PurchaseTaxCodes" })
        {
            using var response = await _client.SendAsync(
                HttpMethod.Get,
                $"{endpoint}('{Uri.EscapeDataString(code)}')",
                null,
                ct).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK) return true;
            if (response.StatusCode == HttpStatusCode.NotFound) continue;

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new ServiceLayerException(response.StatusCode, body);
        }
        return false;
    }

    public async Task<string> CreateAsync(TaxCodeDefinition def, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(def);

        var endpoint = EndpointFor(def.Category);
        var payload = new SalesTaxCodePayload
        {
            Code = def.Code,
            Name = def.Name,
            Rate = def.AliquotaIcms,
            TaxGroups = new List<TaxGroupItem>()
        };

        // Modelagem clássica em localização BR: um tax code "pai" + subgrupos (ICMS/IPI/PIS/COFINS/ST).
        // A estrutura exata depende da localização. Enviamos os atributos reconhecidos
        // e protegemos com try/catch por grupo.
        AddGroupIfAny(payload, "IC", def.AliquotaIcms, def.CstIcms);
        AddGroupIfAny(payload, "IP", def.AliquotaIpi, def.CstIpi);
        AddGroupIfAny(payload, "PI", def.AliquotaPis, def.CstPis);
        AddGroupIfAny(payload, "CO", def.AliquotaCofins, def.CstCofins);
        if (def.TemSt)
        {
            AddGroupIfAny(payload, "ST", def.AliquotaStInterna ?? 0m, null);
        }

        try
        {
            var created = await _client.PostJsonAsync<SalesTaxCodePayload>(endpoint, payload, ct).ConfigureAwait(false);
            var finalCode = created?.Code ?? def.Code;
            _logger.LogInformation("TaxCode {Code} criado em {Endpoint}", finalCode, endpoint);
            return finalCode;
        }
        catch (ServiceLayerException ex) when (ex.ResponseBody.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("TaxCode {Code} já existia no SAP; adotando.", def.Code);
            return def.Code;
        }
    }

    private static void AddGroupIfAny(SalesTaxCodePayload payload, string key, decimal rate, string? cst)
    {
        if (rate <= 0m && string.IsNullOrEmpty(cst)) return;
        payload.TaxGroups!.Add(new TaxGroupItem
        {
            Code = key,
            Rate = rate,
            Cst = cst
        });
    }

    private sealed class SalesTaxCodePayload
    {
        [JsonPropertyName("Code")] public string Code { get; set; } = string.Empty;
        [JsonPropertyName("Name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("Rate")] public decimal Rate { get; set; }
        [JsonPropertyName("TaxGroups")] public List<TaxGroupItem>? TaxGroups { get; set; }
    }

    private sealed class TaxGroupItem
    {
        [JsonPropertyName("Code")] public string Code { get; set; } = string.Empty;
        [JsonPropertyName("Rate")] public decimal Rate { get; set; }
        [JsonPropertyName("Cst")] public string? Cst { get; set; }
    }
}
