using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;

namespace MOTORFISCALSAPB1.Worker.Workers;

/// <summary>
/// Recarrega periodicamente o cache de regras fiscais para garantir que
/// mudanças feitas fora deste host sejam percebidas sem reinício.
/// </summary>
public sealed class RuleCacheRefreshWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<RuleCacheRefreshWorker> _logger;
    private readonly TimeSpan _interval;

    public RuleCacheRefreshWorker(
        IServiceProvider sp,
        IConfiguration cfg,
        ILogger<RuleCacheRefreshWorker> logger)
    {
        _sp = sp;
        _logger = logger;
        var minutes = cfg.GetValue<int?>("Worker:RuleCacheRefreshMinutes") ?? 5;
        _interval = TimeSpan.FromMinutes(Math.Max(1, minutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RuleCacheRefreshWorker iniciado. Intervalo={Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<IFiscalRuleCache>();
                var repo = scope.ServiceProvider.GetRequiredService<IFiscalRuleRepository>();
                cache.Invalidate();
                var rules = await cache.GetOrLoadAsync(repo.GetAllActiveAsync, stoppingToken).ConfigureAwait(false);
                _logger.LogDebug("Cache recarregado: {Count} regras", rules.Count);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro recarregando cache.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
        }
    }
}
