using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Fiscal;
using MOTORFISCALSAPB1.Domain.Sap;

namespace MOTORFISCALSAPB1.Application.Services;

/// <summary>
/// Implementação idempotente e thread-safe da criação/garantia do TaxCode:
/// 1) consulta cache persistido (@MF_TAXMAP) pela assinatura;
/// 2) se não existir, adquire lock distribuído por hash (@MF_LOCK);
/// 3) reconsulta dentro do lock (double-check);
/// 4) cria via Service Layer e persiste o mapping.
/// </summary>
public sealed class TaxCodeEnsurer : ITaxCodeEnsurer
{
    private readonly ITaxCodeMappingRepository _mapRepo;
    private readonly ISapTaxCodeService _sap;
    private readonly IDistributedFiscalLock _lock;
    private readonly ITaxCodeCodeGenerator _codeGen;
    private readonly ILogger<TaxCodeEnsurer> _logger;

    public TaxCodeEnsurer(
        ITaxCodeMappingRepository mapRepo,
        ISapTaxCodeService sap,
        IDistributedFiscalLock @lock,
        ITaxCodeCodeGenerator codeGen,
        ILogger<TaxCodeEnsurer> logger)
    {
        _mapRepo = mapRepo;
        _sap = sap;
        _lock = @lock;
        _codeGen = codeGen;
        _logger = logger;
    }

    public async Task<TaxCodeEnsureOutcome> EnsureAsync(
        FiscalSignature signature,
        FiscalResolutionResult result,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(result);

        // 1) cache persistido
        var existing = await _mapRepo.GetByHashAsync(signature.Hash, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return new TaxCodeEnsureOutcome(existing.TaxCode, false, signature.Hash);
        }

        // 2) lock distribuído
        await using var handle = await _lock.AcquireAsync(
            $"taxcode:{signature.Hash}",
            TimeSpan.FromSeconds(30),
            ct).ConfigureAwait(false);

        // 3) double-check
        existing = await _mapRepo.GetByHashAsync(signature.Hash, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return new TaxCodeEnsureOutcome(existing.TaxCode, false, signature.Hash);
        }

        // 4) criação
        var code = _codeGen.Generate(signature);

        // Se já existir no SAP com esse código mas não mapeado, adotamos.
        if (!await _sap.ExistsAsync(code, ct).ConfigureAwait(false))
        {
            var def = BuildDefinition(code, result);
            code = await _sap.CreateAsync(def, ct).ConfigureAwait(false);
            _logger.LogInformation("TaxCode criado no SAP: {Code} (sig {Hash})", code, signature.Hash);
        }
        else
        {
            _logger.LogInformation("TaxCode {Code} já existe no SAP — reutilizando para sig {Hash}", code, signature.Hash);
        }

        var mapping = new TaxCodeMapping(signature.Hash, code, signature.Canonical);
        await _mapRepo.AddAsync(mapping, ct).ConfigureAwait(false);

        return new TaxCodeEnsureOutcome(code, true, signature.Hash);
    }

    private static TaxCodeDefinition BuildDefinition(string code, FiscalResolutionResult r) => new()
    {
        Code = code,
        Name = $"MF {r.Cfop}/{r.CstIcms}/{r.AliquotaIcms:0.##}",
        Category = "O",
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
        AliquotaStInterna = r.AliquotaStInterna,
        MvaSt = r.MvaSt,
        TemDifal = r.TemDifal,
        AliquotaDifal = r.AliquotaDifal,
        AliquotaInterestadual = r.AliquotaInterestadual,
    };
}
