using Microsoft.AspNetCore.SignalR;
using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Api.Hubs;

/// <summary>
/// Adapta <see cref="IProgress{T}"/> para envio de eventos via SignalR.
/// </summary>
public sealed class StructureHubProgress : IProgress<StructureProgressMessage>
{
    private readonly IHubContext<StructureHub> _hub;
    private readonly string? _groupOrClientId;

    public StructureHubProgress(IHubContext<StructureHub> hub, string? groupOrClientId = null)
    {
        _hub = hub;
        _groupOrClientId = groupOrClientId;
    }

    public void Report(StructureProgressMessage value)
    {
        // Fire-and-forget; se houver cliente inscrito recebe.
        _ = Task.Run(() =>
            _hub.Clients.All.SendAsync("progress", value));
    }
}
