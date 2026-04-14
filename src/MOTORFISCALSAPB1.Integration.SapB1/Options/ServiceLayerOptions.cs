namespace MOTORFISCALSAPB1.Integration.SapB1.Options;

/// <summary>
/// Configuracao do SAP Business One Service Layer. Alinhada com o padrao
/// do PortalSapB1 — secao <c>SapServiceLayer</c> no appsettings.
/// </summary>
public sealed class ServiceLayerOptions
{
    public const string SectionName = "SapServiceLayer";

    /// <summary>Ex.: <c>https://sap-host:50000</c> (sem sufixo /b1s/v1).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Company DB, ex.: <c>SBODEMOBR</c>.</summary>
    public string CompanyDB { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Language code SAP (29 = pt-BR).</summary>
    public int Language { get; set; } = 29;

    public bool IgnoreSslErrors { get; set; }

    /// <summary>Timeout HTTP (segundos).</summary>
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>Numero maximo de tentativas em erros transitorios.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Duracao da sessao (B1SESSION cookie) em minutos. Default SL: 30.</summary>
    public int SessionTimeoutMinutes { get; set; } = 25;

    /// <summary>Retorna a URL base normalizada com sufixo <c>/b1s/v1/</c>.</summary>
    public string BuildApiBaseUrl()
    {
        var b = BaseUrl?.TrimEnd('/') ?? string.Empty;
        return b.EndsWith("/b1s/v1", System.StringComparison.OrdinalIgnoreCase)
            ? b + "/"
            : b + "/b1s/v1/";
    }
}
