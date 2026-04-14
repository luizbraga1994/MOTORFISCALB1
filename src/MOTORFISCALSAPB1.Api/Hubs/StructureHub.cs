using Microsoft.AspNetCore.SignalR;

namespace MOTORFISCALSAPB1.Api.Hubs;

/// <summary>
/// Hub SignalR usado para transmitir o progresso da instalação de estrutura.
/// </summary>
public sealed class StructureHub : Hub
{
    public const string Path = "/hubs/structure";
}
