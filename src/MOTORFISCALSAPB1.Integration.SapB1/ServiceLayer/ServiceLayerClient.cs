using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MOTORFISCALSAPB1.Integration.SapB1.Options;

namespace MOTORFISCALSAPB1.Integration.SapB1.ServiceLayer;

public sealed class ServiceLayerClient : IServiceLayerClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly ServiceLayerOptions _options;
    private readonly ILogger<ServiceLayerClient> _logger;
    private readonly SemaphoreSlim _loginGate = new(1, 1);

    private string? _sessionId;
    private DateTime _sessionExpiresAtUtc = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null, // SAP SL usa nomes como "CardCode"
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ServiceLayerClient(HttpClient http, IOptions<ServiceLayerOptions> options, ILogger<ServiceLayerClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("ServiceLayer:BaseUrl não configurado.");
        }

        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativeUrl,
        HttpContent? content,
        CancellationToken ct)
    {
        await EnsureLoggedInAsync(ct).ConfigureAwait(false);

        using var request = new HttpRequestMessage(method, relativeUrl);
        if (content is not null) request.Content = content;
        AttachSessionCookie(request);

        var response = await _http.SendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Sessão Service Layer expirou; renovando.");
            InvalidateSession();
            await EnsureLoggedInAsync(ct).ConfigureAwait(false);

            using var retry = new HttpRequestMessage(method, relativeUrl);
            if (content is not null) retry.Content = content;
            AttachSessionCookie(retry);
            response = await _http.SendAsync(retry, ct).ConfigureAwait(false);
        }

        return response;
    }

    public async Task<T?> GetJsonAsync<T>(string relativeUrl, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, relativeUrl, null, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new ServiceLayerException(response.StatusCode, body);
        }
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts, ct).ConfigureAwait(false);
    }

    public async Task<T?> PostJsonAsync<T>(string relativeUrl, object payload, CancellationToken ct)
    {
        using var content = JsonContent.Create(payload, options: JsonOpts);
        using var response = await SendAsync(HttpMethod.Post, relativeUrl, content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new ServiceLayerException(response.StatusCode, body);
        }
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts, ct).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> PatchJsonAsync(string relativeUrl, object payload, CancellationToken ct)
    {
        using var content = JsonContent.Create(payload, options: JsonOpts);
        return await SendAsync(new HttpMethod("PATCH"), relativeUrl, content, ct).ConfigureAwait(false);
    }

    public Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct)
        => SendAsync(HttpMethod.Delete, relativeUrl, null, ct);

    private async Task EnsureLoggedInAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_sessionId) && DateTime.UtcNow < _sessionExpiresAtUtc)
        {
            return;
        }

        await _loginGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(_sessionId) && DateTime.UtcNow < _sessionExpiresAtUtc)
            {
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "Login");
            request.Content = JsonContent.Create(new
            {
                CompanyDB = _options.CompanyDb,
                UserName = _options.UserName,
                Password = _options.Password
            }, options: JsonOpts);

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new ServiceLayerException(response.StatusCode, "Falha no login Service Layer: " + body);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            _sessionId = doc.RootElement.GetProperty("SessionId").GetString();
            _sessionExpiresAtUtc = DateTime.UtcNow.AddMinutes(_options.SessionTimeoutMinutes);
            _logger.LogInformation("Login Service Layer OK. SessionId={SessionId}", _sessionId?[..Math.Min(8, _sessionId.Length)]);
        }
        finally
        {
            _loginGate.Release();
        }
    }

    private void AttachSessionCookie(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_sessionId))
        {
            request.Headers.Add("Cookie", $"B1SESSION={_sessionId}");
        }
    }

    private void InvalidateSession()
    {
        _sessionId = null;
        _sessionExpiresAtUtc = DateTime.MinValue;
    }

    public void Dispose() => _loginGate.Dispose();
}

public sealed class ServiceLayerException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }
    public ServiceLayerException(System.Net.HttpStatusCode code, string body)
        : base($"Service Layer error {(int)code}: {body}")
    {
        StatusCode = code;
        ResponseBody = body ?? string.Empty;
    }
}
