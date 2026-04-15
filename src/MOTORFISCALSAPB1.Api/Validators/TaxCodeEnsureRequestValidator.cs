using FluentValidation;
using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Api.Validators;

/// <summary>
/// Validacao de entrada para /api/fiscal/taxcode/ensure.
/// Garante os campos minimos exigidos pela assinatura fiscal (CFOP+CST ICMS)
/// e rejeita aliquotas negativas ou fora de faixa razoavel.
/// </summary>
public sealed class TaxCodeEnsureRequestValidator : AbstractValidator<TaxCodeEnsureRequest>
{
    public TaxCodeEnsureRequestValidator()
    {
        RuleFor(x => x.Cfop)
            .NotEmpty().WithMessage("CFOP obrigatorio.")
            .Length(4).WithMessage("CFOP deve ter exatamente 4 digitos.")
            .Matches(@"^[1-7]\d{3}$").WithMessage("CFOP invalido (primeiro digito 1-7).");

        RuleFor(x => x.CstIcms)
            .NotEmpty().WithMessage("CST ICMS obrigatorio.")
            .MaximumLength(3);

        // Aliquotas: percentuais entre 0 e 100. Aceita decimal em 4 casas.
        RuleFor(x => x.AliquotaIcms).InclusiveBetween(0m, 100m);
        RuleFor(x => x.AliquotaIpi).InclusiveBetween(0m, 100m);
        RuleFor(x => x.AliquotaPis).InclusiveBetween(0m, 100m);
        RuleFor(x => x.AliquotaCofins).InclusiveBetween(0m, 100m);

        RuleFor(x => x.CstIpi).MaximumLength(3);
        RuleFor(x => x.CstPis).MaximumLength(3);
        RuleFor(x => x.CstCofins).MaximumLength(3);

        // ST: se flagado, aliquota e MVA devem estar coerentes.
        When(x => x.TemSt, () =>
        {
            RuleFor(x => x.AliquotaStInterna)
                .NotNull().WithMessage("AliquotaStInterna obrigatoria quando TemSt=true.")
                .InclusiveBetween(0m, 100m);
            RuleFor(x => x.MvaSt)
                .NotNull().WithMessage("MvaSt obrigatoria quando TemSt=true.")
                .InclusiveBetween(0m, 1000m); // MVA pode passar de 100% em alguns produtos
        });
    }
}
