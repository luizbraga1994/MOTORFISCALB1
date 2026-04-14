using MOTORFISCALSAPB1.Domain.Common;

namespace MOTORFISCALSAPB1.Domain.Fiscal;

/// <summary>
/// Regra fiscal persistida em UDT (<c>@MF_RULE</c>) e consumida pelo motor.
/// </summary>
public class FiscalRule : Entity<string>
{
    public string Descricao { get; private set; } = string.Empty;
    public int Prioridade { get; private set; }
    public bool Ativo { get; private set; }

    public string? CardCode { get; private set; }
    public string? ItemCode { get; private set; }
    public int? BplId { get; private set; }
    public string? Ncm { get; private set; }
    public string? TipoOperacao { get; private set; }
    public string? UfOrigem { get; private set; }
    public string? UfDestino { get; private set; }
    public bool? ContribuinteIcms { get; private set; }
    public bool? ConsumidorFinal { get; private set; }
    public int? RegimeTributarioFilial { get; private set; }

    public FiscalRuleResult Resultado { get; private set; } = new();

    public DateTime VigenciaInicio { get; private set; }
    public DateTime? VigenciaFim { get; private set; }

    private FiscalRule() : base(string.Empty) { }

    public FiscalRule(
        string id,
        string descricao,
        int prioridade,
        FiscalRuleResult resultado,
        DateTime vigenciaInicio) : base(id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Id da regra é obrigatório.", "RULE_ID_REQUIRED");
        }

        Descricao = descricao ?? string.Empty;
        Prioridade = prioridade;
        Ativo = true;
        Resultado = resultado ?? throw new DomainException("Resultado é obrigatório.", "RULE_RESULT_REQUIRED");
        VigenciaInicio = vigenciaInicio;
    }

    public void DefinirEscopo(
        string? cardCode,
        string? itemCode,
        int? bplId,
        string? ncm,
        string? tipoOperacao,
        string? ufOrigem,
        string? ufDestino,
        bool? contribuinteIcms,
        bool? consumidorFinal,
        int? regimeTributarioFilial)
    {
        CardCode = cardCode;
        ItemCode = itemCode;
        BplId = bplId;
        Ncm = ncm;
        TipoOperacao = tipoOperacao;
        UfOrigem = ufOrigem;
        UfDestino = ufDestino;
        ContribuinteIcms = contribuinteIcms;
        ConsumidorFinal = consumidorFinal;
        RegimeTributarioFilial = regimeTributarioFilial;
    }

    public void Desativar() => Ativo = false;
    public void Reativar() => Ativo = true;

    /// <summary>
    /// Calcula quão específica é a regra. Maior = mais específica.
    /// Usado como critério de desempate determinístico.
    /// </summary>
    public int Especificidade()
    {
        var score = 0;
        if (!string.IsNullOrEmpty(CardCode)) score += 100;
        if (!string.IsNullOrEmpty(ItemCode)) score += 100;
        if (BplId.HasValue) score += 50;
        if (!string.IsNullOrEmpty(Ncm)) score += 20;
        if (!string.IsNullOrEmpty(TipoOperacao)) score += 10;
        if (!string.IsNullOrEmpty(UfOrigem)) score += 5;
        if (!string.IsNullOrEmpty(UfDestino)) score += 5;
        if (ContribuinteIcms.HasValue) score += 3;
        if (ConsumidorFinal.HasValue) score += 3;
        if (RegimeTributarioFilial.HasValue) score += 2;
        return score;
    }

    public bool EstaVigente(DateTime agora)
    {
        if (!Ativo) return false;
        if (agora < VigenciaInicio) return false;
        if (VigenciaFim.HasValue && agora > VigenciaFim.Value) return false;
        return true;
    }

    public bool Matches(FiscalResolutionContext ctx)
    {
        if (!string.IsNullOrEmpty(CardCode) && !string.Equals(CardCode, ctx.CardCode, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(ItemCode) && !string.Equals(ItemCode, ctx.ItemCode, StringComparison.OrdinalIgnoreCase))
            return false;
        if (BplId.HasValue && BplId.Value != ctx.BplId)
            return false;
        if (!string.IsNullOrEmpty(Ncm) && !string.Equals(Ncm, ctx.Ncm, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(TipoOperacao) && !string.Equals(TipoOperacao, ctx.TipoOperacao, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(UfOrigem) && !string.Equals(UfOrigem, ctx.UfOrigem, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(UfDestino) && !string.Equals(UfDestino, ctx.UfDestino, StringComparison.OrdinalIgnoreCase))
            return false;
        if (ContribuinteIcms.HasValue && ContribuinteIcms.Value != ctx.ContribuinteICMS)
            return false;
        if (ConsumidorFinal.HasValue && ConsumidorFinal.Value != ctx.ConsumidorFinal)
            return false;
        if (RegimeTributarioFilial.HasValue && RegimeTributarioFilial.Value != ctx.RegimeTributarioFilial)
            return false;

        return true;
    }
}

public class FiscalRuleResult
{
    public string Cfop { get; set; } = string.Empty;
    public string CstIcms { get; set; } = string.Empty;
    public decimal AliquotaIcms { get; set; }
    public decimal? ReducaoBaseIcms { get; set; }
    public string CstIpi { get; set; } = string.Empty;
    public decimal AliquotaIpi { get; set; }
    public string CstPis { get; set; } = string.Empty;
    public decimal AliquotaPis { get; set; }
    public string CstCofins { get; set; } = string.Empty;
    public decimal AliquotaCofins { get; set; }
    public bool TemSt { get; set; }
    public decimal? MvaSt { get; set; }
    public decimal? AliquotaStInterna { get; set; }
    public bool TemDifal { get; set; }
    public decimal? AliquotaDifal { get; set; }
    public decimal? AliquotaInterestadual { get; set; }
}
