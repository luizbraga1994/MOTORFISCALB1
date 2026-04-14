using System.Net;
using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Integration.SapB1.ServiceLayer;
using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Integration.SapB1.Structure;

public interface IUserObjectsMdService
{
    Task<bool> ExistsAsync(string code, CancellationToken ct);
    Task<bool> EnsureAsync(UserObjectDefinition definition, CancellationToken ct);
}

public sealed class UserObjectsMdService : IUserObjectsMdService
{
    private readonly IServiceLayerClient _client;
    private readonly ILogger<UserObjectsMdService> _logger;

    public UserObjectsMdService(IServiceLayerClient client, ILogger<UserObjectsMdService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(string code, CancellationToken ct)
    {
        using var response = await _client.SendAsync(
            HttpMethod.Get,
            $"UserObjectsMD('{Uri.EscapeDataString(code)}')",
            null,
            ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.OK) return true;
        if (response.StatusCode == HttpStatusCode.NotFound) return false;

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        throw new ServiceLayerException(response.StatusCode, body);
    }

    public async Task<bool> EnsureAsync(UserObjectDefinition def, CancellationToken ct)
    {
        if (await ExistsAsync(def.Code, ct).ConfigureAwait(false))
        {
            _logger.LogDebug("UDO {Code} já existe.", def.Code);
            return false;
        }

        var dto = new UserObjectDto
        {
            Code = def.Code,
            Name = def.Name,
            TableName = def.TableName,
            ObjectType = def.ObjectType,
            CanCancel = def.CanCancel ? "tYES" : "tNO",
            CanDelete = def.CanDelete ? "tYES" : "tNO",
            CanLog = def.CanLog ? "tYES" : "tNO",
            CanFind = def.CanFind ? "tYES" : "tNO",
            ManageSeries = def.ManageSeries ? "tYES" : "tNO",
            ChildTables = def.ChildTables?
                .Select((t, i) => new UserObjectChildTableDto { SonNumber = i, TableName = t })
                .ToList(),
            FindColumns = def.FindColumns?
                .Select(c => new UserObjectFindColumnDto
                {
                    ColumnAlias = c.Alias,
                    ColumnDescription = c.Description
                }).ToList()
        };

        await _client.PostJsonAsync<object>("UserObjectsMD", dto, ct).ConfigureAwait(false);
        _logger.LogInformation("UDO criado: {Code}", def.Code);
        return true;
    }
}
