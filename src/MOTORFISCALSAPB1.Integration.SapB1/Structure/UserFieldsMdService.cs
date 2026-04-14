using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Integration.SapB1.ServiceLayer;
using MOTORFISCALSAPB1.Shared.Contracts;
using MOTORFISCALSAPB1.Shared.Helpers;

namespace MOTORFISCALSAPB1.Integration.SapB1.Structure;

public interface IUserFieldsMdService
{
    Task<bool> EnsureAsync(UserFieldDefinition definition, CancellationToken ct);
}

/// <summary>
/// Criação de UDFs via Service Layer <c>/UserFieldsMD</c>.
/// Validação de existência usa <c>CUFD</c> via HANA (ISapReadPort).
/// </summary>
public sealed class UserFieldsMdService : IUserFieldsMdService
{
    private readonly IServiceLayerClient _client;
    private readonly ISapReadPort _sapRead;
    private readonly ILogger<UserFieldsMdService> _logger;

    public UserFieldsMdService(
        IServiceLayerClient client,
        ISapReadPort sapRead,
        ILogger<UserFieldsMdService> logger)
    {
        _client = client;
        _sapRead = sapRead;
        _logger = logger;
    }

    public async Task<bool> EnsureAsync(UserFieldDefinition def, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(def);

        var tableIdForCufd = SapNamingConventions.NormalizeTableId(def.Table); // com '@' para UDT, sem para padrão
        var alias = SapNamingConventions.NormalizeFieldAlias(def.Name);        // sem 'U_'

        if (await _sapRead.UserFieldExistsAsync(tableIdForCufd, alias, ct).ConfigureAwait(false))
        {
            _logger.LogDebug("UDF já existe: {Table}.U_{Alias}. Pulando.", tableIdForCufd, alias);
            return false;
        }

        // Service Layer espera TableName sem '@' para UDTs.
        var tableForSl = SapNamingConventions.IsStandardTable(def.Table)
            ? tableIdForCufd
            : SapNamingConventions.StripUserTablePrefix(tableIdForCufd);

        var dto = new UserFieldDto
        {
            TableName = tableForSl,
            Name = alias,
            Description = def.Description,
            Type = def.FieldType,
            SubType = def.SubType,
            EditSize = def.Size,
            Mandatory = def.Mandatory ? "tYES" : "tNO",
            DefaultValue = def.DefaultValue,
            LinkedTable = def.LinkedTable,
            ValidValuesMD = def.ValidValues?.Select(v => new UserFieldValidValueDto
            {
                Value = v.Value,
                Description = v.Description
            }).ToList()
        };

        await _client.PostJsonAsync<object>("UserFieldsMD", dto, ct).ConfigureAwait(false);
        _logger.LogInformation("UDF criado: {Table}.U_{Alias}", tableForSl, alias);
        return true;
    }
}
