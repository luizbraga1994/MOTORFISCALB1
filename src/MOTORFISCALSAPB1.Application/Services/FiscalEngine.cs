using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Services;

public sealed class FiscalEngine : IFiscalEngine
{
    private readonly IFiscalRuleResolver _resolver;
    private readonly IFiscalSignatureService _signatureService;
    private readonly ITaxCodeEnsurer _ensurer;
    private readonly IDifalCalculator _difal;
    private readonly IFiscalAuditRepository _audit;
    private readonly ILogger<FiscalEngine> _logger;

    public FiscalEngine(
        IFiscalRuleResolver resolver,
        IFiscalSignatureService signatureService,
        ITaxCodeEnsurer ensurer,
        IDifalCalculator difal,
        IFiscalAuditRepository audit,
        ILogger<FiscalEngine> logger)
    {
        _resolver = resolver;
        _signatureService = signatureService;
        _ensurer = ensurer;
        _difal = difal;
        _audit = audit;
        _logger = logger;
    }

    public async Task<FiscalResolutionResult> ResolveAsync(FiscalResolutionContext context, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var result = new FiscalResolutionResult();
        string? error = null;
        FiscalRule? rule = null;
        FiscalSignature? signature = null;
        TaxCodeEnsureOutcome? outcome = null;

        try
        {
            rule = await _resolver.ResolveAsync(context, ct).ConfigureAwait(false);
            ApplyRule(rule, result);
            _difal.Apply(context, result);

            signature = _signatureService.Build(result);
            result.AssinaturaFiscal = signature.Hash;

            outcome = await _ensurer.EnsureAsync(signature, result, ct).ConfigureAwait(false);
            result.TaxCode = outcome.TaxCode;
            result.TaxCodeCriadoAgora = outcome.Criado;

            return result;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger.LogError(ex, "Falha no motor fiscal. BP={BP} Item={Item}", context.CardCode, context.ItemCode);
            throw;
        }
        finally
        {
            sw.Stop();
            try
            {
                var entry = new FiscalAuditEntry(
                    Guid.NewGuid().ToString("N"),
                    context.CorrelationId,
                    userName: null,
                    context.CardCode,
                    context.BplId,
                    context.ItemCode,
                    rule?.Id,
                    signature?.Hash,
                    outcome?.TaxCode,
                    outcome?.Criado ?? false,
                    sw.ElapsedMilliseconds,
                    JsonSerializer.Serialize(context),
                    JsonSerializer.Serialize(result),
                    error);
                await _audit.AddAsync(entry, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception auditEx)
            {
                // Auditoria e best-effort: falha nao pode mascarar nem abortar a
                // operacao fiscal principal. Mas precisa ter contexto suficiente
                // para forensic post-mortem (qual resolucao, qual hash, qual tax
                // code nao ficou auditado). Log em Warning com campos estruturados.
                _logger.LogWarning(auditEx,
                    "Falha ao registrar auditoria fiscal. CorrelationId={CorrelationId} BP={BP} BPL={BPL} Item={Item} Rule={RuleId} Hash={Hash} TaxCode={TaxCode} Duration={DurationMs}ms OriginalError={OriginalError}",
                    context.CorrelationId,
                    context.CardCode,
                    context.BplId,
                    context.ItemCode,
                    rule?.Id,
                    signature?.Hash,
                    outcome?.TaxCode,
                    sw.ElapsedMilliseconds,
                    error);
            }
        }
    }

    public async Task<FiscalResolutionResult> SimulateAsync(FiscalResolutionContext context, CancellationToken ct)
    {
        var rule = await _resolver.ResolveAsync(context, ct).ConfigureAwait(false);
        var result = new FiscalResolutionResult();
        ApplyRule(rule, result);
        _difal.Apply(context, result);
        var signature = _signatureService.Build(result);
        result.AssinaturaFiscal = signature.Hash;
        return result;
    }

    private static void ApplyRule(FiscalRule rule, FiscalResolutionResult r)
    {
        var src = rule.Resultado;
        r.Cfop = src.Cfop;
        r.CstIcms = src.CstIcms;
        r.AliquotaIcms = src.AliquotaIcms;
        r.ReducaoBaseIcms = src.ReducaoBaseIcms;
        r.CstIpi = src.CstIpi;
        r.AliquotaIpi = src.AliquotaIpi;
        r.CstPis = src.CstPis;
        r.AliquotaPis = src.AliquotaPis;
        r.CstCofins = src.CstCofins;
        r.AliquotaCofins = src.AliquotaCofins;
        r.TemSt = src.TemSt;
        r.MvaSt = src.MvaSt;
        r.AliquotaStInterna = src.AliquotaStInterna;
        r.TemDifal = src.TemDifal;
        r.AliquotaDifal = src.AliquotaDifal;
        r.AliquotaInterestadual = src.AliquotaInterestadual;
        r.RegraAplicada = rule.Id;
    }
}
