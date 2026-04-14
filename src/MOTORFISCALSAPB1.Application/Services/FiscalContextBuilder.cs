using Microsoft.Extensions.Logging;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Common;
using MOTORFISCALSAPB1.Domain.Fiscal;
using MOTORFISCALSAPB1.Shared.Contracts;

namespace MOTORFISCALSAPB1.Application.Services;

public sealed class FiscalContextBuilder : IFiscalContextBuilder
{
    private readonly ISapReadPort _sap;
    private readonly ILogger<FiscalContextBuilder> _logger;

    public FiscalContextBuilder(ISapReadPort sap, ILogger<FiscalContextBuilder> logger)
    {
        _sap = sap;
        _logger = logger;
    }

    public async Task<FiscalResolutionContext> BuildAsync(FiscalResolutionRequest req, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);

        if (string.IsNullOrWhiteSpace(req.CardCode))
            throw new DomainException("CardCode obrigatório.", "CTX_CARDCODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(req.ItemCode))
            throw new DomainException("ItemCode obrigatório.", "CTX_ITEMCODE_REQUIRED");
        if (req.BplId <= 0)
            throw new DomainException("BPLId obrigatório.", "CTX_BPLID_REQUIRED");

        var bpTask = _sap.GetBusinessPartnerAsync(req.CardCode, ct);
        var branchTask = _sap.GetBranchAsync(req.BplId, ct);
        var itemTask = _sap.GetItemAsync(req.ItemCode, ct);

        await Task.WhenAll(bpTask, branchTask, itemTask).ConfigureAwait(false);

        var bp = bpTask.Result ?? throw new DomainException($"BP {req.CardCode} não encontrado.", "CTX_BP_NOT_FOUND");
        var branch = branchTask.Result ?? throw new DomainException($"Filial {req.BplId} não encontrada.", "CTX_BRANCH_NOT_FOUND");
        var item = itemTask.Result ?? throw new DomainException($"Item {req.ItemCode} não encontrado.", "CTX_ITEM_NOT_FOUND");

        var end = bp.EnderecoEntrega ?? bp.EnderecoCobranca;

        var ctx = new FiscalResolutionContext
        {
            CardCode = bp.CardCode,
            CardType = bp.CardType,
            Cnpj = bp.LicTradNum,
            InscricaoEstadual = bp.InscricaoEstadual,
            ContribuinteICMS = bp.ContribuinteIcms,
            // ConsumidorFinal vem do doc (IndFinal), repassado pelo caller no request.
            ConsumidorFinal = req.ConsumidorFinal,
            UfDestino = end?.State ?? string.Empty,
            CidadeDestino = end?.City ?? string.Empty,
            BplId = branch.BplId,
            UfOrigem = branch.Uf,
            RegimeTributarioFilial = branch.RegimeTributario,
            AtividadeFilial = branch.Atividade,
            ItemCode = item.ItemCode,
            Ncm = item.Ncm,
            Cest = item.Cest,
            OrigemMercadoria = item.OrigemMercadoria,
            TipoOperacao = req.TipoOperacao.ToString(),
            CorrelationId = req.CorrelationId
        };

        _logger.LogDebug(
            "Contexto fiscal montado: BP={CardCode} BPL={BplId} Item={ItemCode} UF {UfO}->{UfD} Cons.Final={CF} Contrib={Ctr}",
            ctx.CardCode, ctx.BplId, ctx.ItemCode, ctx.UfOrigem, ctx.UfDestino, ctx.ConsumidorFinal, ctx.ContribuinteICMS);

        return ctx;
    }
}
