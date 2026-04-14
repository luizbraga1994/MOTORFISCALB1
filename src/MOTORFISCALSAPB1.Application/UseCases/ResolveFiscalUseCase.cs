using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Application.UseCases;

public interface IResolveFiscalUseCase
{
    Task<FiscalResolutionResponse> HandleAsync(FiscalResolutionRequest request, CancellationToken ct);
    Task<FiscalResolutionResponse> SimulateAsync(FiscalResolutionRequest request, CancellationToken ct);
}

public sealed class ResolveFiscalUseCase : IResolveFiscalUseCase
{
    private readonly IFiscalContextBuilder _builder;
    private readonly IFiscalEngine _engine;

    public ResolveFiscalUseCase(IFiscalContextBuilder builder, IFiscalEngine engine)
    {
        _builder = builder;
        _engine = engine;
    }

    public async Task<FiscalResolutionResponse> HandleAsync(FiscalResolutionRequest request, CancellationToken ct)
    {
        var ctx = await _builder.BuildAsync(request, ct).ConfigureAwait(false);
        var result = await _engine.ResolveAsync(ctx, ct).ConfigureAwait(false);
        return Map(result, request.CorrelationId);
    }

    public async Task<FiscalResolutionResponse> SimulateAsync(FiscalResolutionRequest request, CancellationToken ct)
    {
        var ctx = await _builder.BuildAsync(request, ct).ConfigureAwait(false);
        var result = await _engine.SimulateAsync(ctx, ct).ConfigureAwait(false);
        return Map(result, request.CorrelationId);
    }

    private static FiscalResolutionResponse Map(Domain.Fiscal.FiscalResolutionResult r, string? correlationId) => new()
    {
        TaxCode = r.TaxCode ?? string.Empty,
        Cfop = r.Cfop,
        CstIcms = r.CstIcms,
        AliquotaIcms = r.AliquotaIcms,
        ReducaoBaseIcms = r.ReducaoBaseIcms,
        CstIpi = r.CstIpi,
        AliquotaIpi = r.AliquotaIpi,
        CstPis = r.CstPis,
        AliquotaPis = r.AliquotaPis,
        CstCofins = r.CstCofins,
        AliquotaCofins = r.AliquotaCofins,
        TemSt = r.TemSt,
        MvaSt = r.MvaSt,
        AliquotaStInterna = r.AliquotaStInterna,
        TemDifal = r.TemDifal,
        AliquotaDifal = r.AliquotaDifal,
        AliquotaInterestadual = r.AliquotaInterestadual,
        RegraAplicada = r.RegraAplicada,
        AssinaturaFiscal = r.AssinaturaFiscal,
        TaxCodeCriadoAgora = r.TaxCodeCriadoAgora,
        CorrelationId = correlationId
    };
}
