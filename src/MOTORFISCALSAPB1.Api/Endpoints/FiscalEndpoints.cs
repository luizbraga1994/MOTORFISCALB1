using FluentValidation;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Application.UseCases;
using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Api.Endpoints;

public static class FiscalEndpoints
{
    public static IEndpointRouteBuilder MapFiscalEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/fiscal").WithTags("Fiscal");

        g.MapPost("/resolve", async (
            FiscalResolutionRequest req,
            IResolveFiscalUseCase uc,
            IValidator<FiscalResolutionRequest> validator,
            HttpContext http,
            CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            req.CorrelationId ??= http.Items["CorrelationId"] as string;
            var resp = await uc.HandleAsync(req, ct);
            return Results.Ok(resp);
        }).WithName("ResolveFiscal");

        g.MapPost("/simulate", async (
            FiscalResolutionRequest req,
            IResolveFiscalUseCase uc,
            IValidator<FiscalResolutionRequest> validator,
            HttpContext http,
            CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            req.CorrelationId ??= http.Items["CorrelationId"] as string;
            var resp = await uc.SimulateAsync(req, ct);
            return Results.Ok(resp);
        }).WithName("SimulateFiscal");

        g.MapPost("/taxcode/ensure", async (
            TaxCodeEnsureRequest req,
            IFiscalSignatureService signatureSvc,
            ITaxCodeEnsurer ensurer,
            CancellationToken ct) =>
        {
            var result = new Domain.Fiscal.FiscalResolutionResult
            {
                Cfop = req.Cfop,
                CstIcms = req.CstIcms,
                AliquotaIcms = req.AliquotaIcms,
                CstIpi = req.CstIpi,
                AliquotaIpi = req.AliquotaIpi,
                CstPis = req.CstPis,
                AliquotaPis = req.AliquotaPis,
                CstCofins = req.CstCofins,
                AliquotaCofins = req.AliquotaCofins,
                TemSt = req.TemSt,
                AliquotaStInterna = req.AliquotaStInterna,
                MvaSt = req.MvaSt
            };
            var sig = signatureSvc.Build(result);
            var outcome = await ensurer.EnsureAsync(sig, result, ct);
            return Results.Ok(new TaxCodeEnsureResponse
            {
                TaxCode = outcome.TaxCode,
                AssinaturaFiscal = outcome.SignatureHash,
                Criado = outcome.Criado
            });
        }).WithName("EnsureTaxCode");

        return app;
    }
}
