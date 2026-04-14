namespace MOTORFISCALSAPB1.Integration.SapB1.ServiceLayer;

/// <summary>
/// Cliente HTTP tipado para o Service Layer do SAP B1 — faz login/logout e
/// gerencia sessão de forma thread-safe.
/// </summary>
public interface IServiceLayerClient
{
    Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativeUrl,
        HttpContent? content,
        CancellationToken ct);

    Task<T?> GetJsonAsync<T>(string relativeUrl, CancellationToken ct);
    Task<T?> PostJsonAsync<T>(string relativeUrl, object payload, CancellationToken ct);
    Task<HttpResponseMessage> PatchJsonAsync(string relativeUrl, object payload, CancellationToken ct);
    Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct);
}
