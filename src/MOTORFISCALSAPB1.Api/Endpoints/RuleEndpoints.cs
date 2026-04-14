using MOTORFISCALSAPB1.Application.UseCases;
using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Api.Endpoints;

public static class RuleEndpoints
{
    public static IEndpointRouteBuilder MapRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/rules").WithTags("Rules");

        g.MapGet("/", async (IManageRuleUseCase uc, CancellationToken ct) =>
        {
            var list = await uc.ListAsync(ct);
            return Results.Ok(list);
        }).WithName("ListRules");

        g.MapGet("/{id}", async (string id, IManageRuleUseCase uc, CancellationToken ct) =>
        {
            var rule = await uc.GetAsync(id, ct);
            return rule is null ? Results.NotFound() : Results.Ok(rule);
        }).WithName("GetRule");

        g.MapPut("/", async (FiscalRuleDto dto, IManageRuleUseCase uc, CancellationToken ct) =>
        {
            var rule = dto.ToDomain();
            await uc.UpsertAsync(rule, ct);
            return Results.NoContent();
        }).WithName("UpsertRule");

        g.MapDelete("/{id}", async (string id, IManageRuleUseCase uc, CancellationToken ct) =>
        {
            await uc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).WithName("DeleteRule");

        return app;
    }
}

public sealed class FiscalRuleDto
{
    public string Id { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int Prioridade { get; set; }
    public DateTime VigenciaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? VigenciaFim { get; set; }
    public string? CardCode { get; set; }
    public string? ItemCode { get; set; }
    public int? BplId { get; set; }
    public string? Ncm { get; set; }
    public string? TipoOperacao { get; set; }
    public string? UfOrigem { get; set; }
    public string? UfDestino { get; set; }
    public bool? ContribuinteIcms { get; set; }
    public bool? ConsumidorFinal { get; set; }
    public int? RegimeTributarioFilial { get; set; }
    public FiscalRuleResult Resultado { get; set; } = new();

    public FiscalRule ToDomain()
    {
        var rule = new FiscalRule(Id, Descricao, Prioridade, Resultado, VigenciaInicio);
        rule.DefinirEscopo(CardCode, ItemCode, BplId, Ncm, TipoOperacao, UfOrigem, UfDestino,
            ContribuinteIcms, ConsumidorFinal, RegimeTributarioFilial);
        return rule;
    }
}
