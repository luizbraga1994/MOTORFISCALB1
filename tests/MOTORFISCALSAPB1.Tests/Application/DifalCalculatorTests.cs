using FluentAssertions;
using MOTORFISCALSAPB1.Application.Services;
using MOTORFISCALSAPB1.Domain.Fiscal;
using Xunit;

namespace MOTORFISCALSAPB1.Tests.Application;

public class DifalCalculatorTests
{
    [Fact]
    public void Nao_calcula_para_operacao_interna()
    {
        var ctx = new FiscalResolutionContext { UfOrigem = "SP", UfDestino = "SP", ConsumidorFinal = true };
        var r = new FiscalResolutionResult { AliquotaIcms = 18m };
        new DifalCalculator().Apply(ctx, r);
        r.TemDifal.Should().BeFalse();
    }

    [Fact]
    public void Nao_calcula_se_nao_for_consumidor_final()
    {
        var ctx = new FiscalResolutionContext { UfOrigem = "SP", UfDestino = "RJ", ConsumidorFinal = false };
        var r = new FiscalResolutionResult { AliquotaIcms = 12m };
        new DifalCalculator().Apply(ctx, r);
        r.TemDifal.Should().BeFalse();
    }

    [Fact]
    public void Calcula_diferenca_entre_aliquotas()
    {
        var ctx = new FiscalResolutionContext { UfOrigem = "SP", UfDestino = "RJ", ConsumidorFinal = true };
        var r = new FiscalResolutionResult
        {
            AliquotaIcms = 18m,
            AliquotaInterestadual = 12m
        };
        new DifalCalculator().Apply(ctx, r);
        r.TemDifal.Should().BeTrue();
        r.AliquotaDifal.Should().Be(6m);
        r.AliquotaInterestadual.Should().Be(12m);
    }
}
