using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Common;
using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Services;

/// <summary>
/// Resolve a regra fiscal aplicável ao contexto seguindo a prioridade:
/// 1) parceiro + item + filial,
/// 2) item + operação + UF origem/destino,
/// 3) NCM + operação + perfil fiscal,
/// 4) genérica,
/// 5) fallback controlado.
/// </summary>
public interface IFiscalRuleResolver
{
    Task<FiscalRule> ResolveAsync(FiscalResolutionContext ctx, CancellationToken ct);
}

public sealed class FiscalRuleResolver : IFiscalRuleResolver
{
    private readonly IFiscalRuleRepository _repo;
    private readonly IFiscalRuleCache _cache;
    private readonly ILogger<FiscalRuleResolver> _logger;

    public FiscalRuleResolver(
        IFiscalRuleRepository repo,
        IFiscalRuleCache cache,
        ILogger<FiscalRuleResolver> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FiscalRule> ResolveAsync(FiscalResolutionContext ctx, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var rules = await _cache.GetOrLoadAsync(c => _repo.GetAllActiveAsync(c), ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;

        var matches = rules
            .Where(r => r.EstaVigente(now) && r.Matches(ctx))
            .ToList();

        if (matches.Count == 0)
        {
            throw new DomainException(
                $"Nenhuma regra fiscal aplicável ao contexto (BP={ctx.CardCode}, Item={ctx.ItemCode}, Op={ctx.TipoOperacao}).",
                "RULE_NOT_FOUND");
        }

        // Desempate determinístico: especificidade DESC, prioridade DESC, Id ASC.
        var ordered = matches
            .OrderByDescending(r => r.Especificidade())
            .ThenByDescending(r => r.Prioridade)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

        var winner = ordered[0];

        // Ambiguidade: topo empatado em especificidade+prioridade com outro.
        if (ordered.Count > 1)
        {
            var second = ordered[1];
            if (winner.Especificidade() == second.Especificidade()
                && winner.Prioridade == second.Prioridade)
            {
                _logger.LogWarning(
                    "Ambiguidade de regras fiscais. Vencedora pelo desempate de ID asc: {Winner} vs {Second}. Contexto BP={BP} Item={Item}",
                    winner.Id, second.Id, ctx.CardCode, ctx.ItemCode);
            }
        }

        _logger.LogInformation(
            "Regra aplicada: {RuleId} (esp={Esp}, prio={Prio}) para BP={BP} Item={Item} Op={Op}",
            winner.Id, winner.Especificidade(), winner.Prioridade, ctx.CardCode, ctx.ItemCode, ctx.TipoOperacao);

        return winner;
    }
}
