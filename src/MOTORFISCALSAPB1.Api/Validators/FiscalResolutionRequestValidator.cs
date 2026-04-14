using FluentValidation;
using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Api.Validators;

public sealed class FiscalResolutionRequestValidator : AbstractValidator<FiscalResolutionRequest>
{
    public FiscalResolutionRequestValidator()
    {
        RuleFor(x => x.CardCode).NotEmpty().MaximumLength(15);
        RuleFor(x => x.ItemCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BplId).GreaterThan(0);
        RuleFor(x => x.TipoOperacao).IsInEnum();
    }
}
