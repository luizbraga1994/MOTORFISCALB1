namespace MOTORFISCALSAPB1.Integration.SapB1.Options;

public sealed class ServiceLayerOptions
{
    public const string SectionName = "ServiceLayer";

    /// <summary>Ex: <c>https://sap-host:50000/b1s/v1/</c></summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string CompanyDb { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public bool IgnoreSslErrors { get; set; }
    public int HttpTimeoutSeconds { get; set; } = 60;
    public int SessionTimeoutMinutes { get; set; } = 25;
}
