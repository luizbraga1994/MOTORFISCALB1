using FluentAssertions;
using MOTORFISCALSAPB1.Domain.Fiscal;
using Xunit;

namespace MOTORFISCALSAPB1.Tests.Domain;

public class FiscalRuleTests
{
    private static FiscalRule MakeRule(
        string id = "R1",
        int prioridade = 1,
        string? cardCode = null,
        string? itemCode = null,
        int? bplId = null,
        string? tipoOp = null)
    {
        var r = new FiscalRule(id, id, prioridade, new FiscalRuleResult
        {
            Cfop = "5102", CstIcms = "00", AliquotaIcms = 18m
        }, DateTime.UtcNow.AddDays(-1));
        r.DefinirEscopo(cardCode, itemCode, bplId, null, tipoOp, null, null, null, null, null);
        return r;
    }

    [Fact]
    public void Matches_positivo_quando_escopo_vazio()
    {
        var rule = MakeRule();
        var ctx = new FiscalResolutionContext { CardCode = "C1", ItemCode = "I1", BplId = 1 };
        rule.Matches(ctx).Should().BeTrue();
    }

    [Fact]
    public void Matches_falha_se_cardcode_diferente()
    {
        var rule = MakeRule(cardCode: "C1");
        var ctx = new FiscalResolutionContext { CardCode = "C2", ItemCode = "I1", BplId = 1 };
        rule.Matches(ctx).Should().BeFalse();
    }

    [Fact]
    public void Especificidade_cresce_com_dimensoes_preenchidas()
    {
        var generic = MakeRule();
        var specific = MakeRule(cardCode: "C1", itemCode: "I1", bplId: 1);
        specific.Especificidade().Should().BeGreaterThan(generic.Especificidade());
    }

    [Fact]
    public void Regra_desativada_nao_fica_vigente()
    {
        var r = MakeRule();
        r.Desativar();
        r.EstaVigente(DateTime.UtcNow).Should().BeFalse();
    }
}
