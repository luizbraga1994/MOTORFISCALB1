using System.Net;
using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Integration.SapB1.ServiceLayer;
using MOTORFISCALSAPB1.Shared.Contracts;
using MOTORFISCALSAPB1.Shared.Helpers;

namespace MOTORFISCALSAPB1.Integration.SapB1.Structure;

public interface IUserTablesMdService
{
    Task<bool> ExistsAsync(string tableName, CancellationToken ct);
    Task<bool> EnsureAsync(UserTableDefinition definition, CancellationToken ct);
}

/// <summary>
/// Criação de UDTs via Service Layer (<c>/UserTablesMD</c>).
/// </summary>
public sealed class UserTablesMdService : IUserTablesMdService
{
    private readonly IServiceLayerClient _client;
    private readonly ILogger<UserTablesMdService> _logger;

    public UserTablesMdService(IServiceLayerClient client, ILogger<UserTablesMdService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(string tableName, CancellationToken ct)
    {
        var name = SapNamingConventions.StripUserTablePrefix(tableName);
        using var response = await _client.SendAsync(
            HttpMethod.Get,
            $"UserTablesMD('{Uri.EscapeDataString(name)}')",
            null,
            ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.OK) return true;
        if (response.StatusCode == HttpStatusCode.NotFound) return false;

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        throw new ServiceLayerException(response.StatusCode, body);
    }

    public async Task<bool> EnsureAsync(UserTableDefinition def, CancellationToken ct)
    {
        var name = SapNamingConventions.StripUserTablePrefix(def.Name);
        if (await ExistsAsync(name, ct).ConfigureAwait(false))
        {
            _logger.LogDebug("UDT {Name} já existe. Pulando.", name);
            return false;
        }

        var dto = new UserTableDto
        {
            TableName = name,
            TableDescription = def.Description,
            TableType = def.TableType,
            Archivable = def.Archivable ? "tYES" : "tNO"
        };

        await _client.PostJsonAsync<object>("UserTablesMD", dto, ct).ConfigureAwait(false);
        _logger.LogInformation("UDT criada: {Name} ({Type})", name, def.TableType);
        return true;
    }
}
