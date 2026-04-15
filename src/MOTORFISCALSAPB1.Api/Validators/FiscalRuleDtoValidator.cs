using FluentValidation;
using MOTORFISCALSAPB1.Api.Endpoints;

namespace MOTORFISCALSAPB1.Api.Validators;

/// <summary>
/// Validacao para UPSERT de regras fiscais. Garante:
/// - Id nao vazio e dentro do limite do SAP (@MF_RULE.Code: 50 chars).
/// - Descricao obrigatoria.
/// - Vigencia consistente (fim >= inicio quando informado).
/// - Pelo menos um criterio de escopo preenchido (evita regra "catch-all"
///   acidental que ganharia especificidade 0 e quebraria o funil).
/// - Resultado.Cfop e Resultado.CstIcms obrigatorios (base da assinatura).
/// </summary>
public sealed class FiscalRuleDtoValidator : AbstractValidator<FiscalRuleDto>
{
    public FiscalRuleDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Prioridade).InclusiveBetween(0, 999);

        RuleFor(x => x.VigenciaFim)
            .GreaterThanOrEqualTo(x => x.VigenciaInicio)
            .When(x => x.VigenciaFim.HasValue)
            .WithMessage("VigenciaFim nao pode ser anterior a VigenciaInicio.");

        RuleFor(x => x).Must(HaveAtLeastOneScope)
            .WithMessage("Regra deve ter ao menos 1 criterio de escopo (CardCode, ItemCode, BplId, Ncm, TipoOperacao, UfOrigem, UfDestino, ContribuinteIcms, ConsumidorFinal ou Regime).");

        RuleFor(x => x.Resultado).NotNull();
        RuleFor(x => x.Resultado.Cfop)
            .NotEmpty().WithMessage("Resultado.Cfop obrigatorio.")
            .Length(4).When(x => x.Resultado != null && !string.IsNullOrEmpty(x.Resultado.Cfop));
        RuleFor(x => x.Resultado.CstIcms)
            .NotEmpty().WithMessage("Resultado.CstIcms obrigatorio.")
            .MaximumLength(3).When(x => x.Resultado != null && !string.IsNullOrEmpty(x.Resultado.CstIcms));

        // UF: 2 letras maiusculas quando informada.
        RuleFor(x => x.UfOrigem)
            .Matches(@"^[A-Z]{2}$").When(x => !string.IsNullOrWhiteSpace(x.UfOrigem))
            .WithMessage("UfOrigem deve ter 2 letras maiusculas.");
        RuleFor(x => x.UfDestino)
            .Matches(@"^[A-Z]{2}$").When(x => !string.IsNullOrWhiteSpace(x.UfDestino))
            .WithMessage("UfDestino deve ter 2 letras maiusculas.");

        RuleFor(x => x.Ncm)
            .Matches(@"^\d{8}$").When(x => !string.IsNullOrWhiteSpace(x.Ncm))
            .WithMessage("Ncm deve ter 8 digitos.");
    }

    private static bool HaveAtLeastOneScope(FiscalRuleDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.CardCode)
            || !string.IsNullOrWhiteSpace(dto.ItemCode)
            || dto.BplId.HasValue
            || !string.IsNullOrWhiteSpace(dto.Ncm)
            || !string.IsNullOrWhiteSpace(dto.TipoOperacao)
            || !string.IsNullOrWhiteSpace(dto.UfOrigem)
            || !string.IsNullOrWhiteSpace(dto.UfDestino)
            || dto.ContribuinteIcms.HasValue
            || dto.ConsumidorFinal.HasValue
            || dto.RegimeTributarioFilial.HasValue;
    }
}
