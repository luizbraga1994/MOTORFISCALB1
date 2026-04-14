using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Application.Services;
using MOTORFISCALSAPB1.Domain.Common;
using MOTORFISCALSAPB1.Domain.Fiscal;
using Xunit;

namespace MOTORFISCALSAPB1.Tests.Application;

public class FiscalRuleResolverTests
{
    private static FiscalRule MakeRule(string id, int prio, string? bp = null, string? item = null)
    {
        var r = new FiscalRule(id, id, prio, new FiscalRuleResult
        {
            Cfop = "5102", CstIcms = "00", AliquotaIcms = 18m,
            CstPis = "01", AliquotaPis = 1.65m,
            CstCofins = "01", AliquotaCofins = 7.6m
        }, DateTime.UtcNow.AddDays(-1));
        r.DefinirEscopo(bp, item, null, null, null, null, null, null, null, null);
        return r;
    }

    private static FiscalRuleResolver Build(IReadOnlyList<FiscalRule> rules)
    {
        var repo = new Mock<IFiscalRuleRepository>();
        repo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rules);
        var cache = new Mock<IFiscalRuleCache>();
        cache.Setup(c => c.GetOrLoadAsync(
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<FiscalRule>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IReadOnlyList<FiscalRule>>>, CancellationToken>((loader, ct) => loader(ct));
        return new FiscalRuleResolver(repo.Object, cache.Object, NullLogger<FiscalRuleResolver>.Instance);
    }

    [Fact]
    public async Task Escolhe_regra_mais_especifica()
    {
        var generic = MakeRule("G", 1);
        var specific = MakeRule("S", 1, bp: "C1", item: "I1");
        var resolver = Build(new[] { generic, specific });

        var ctx = new FiscalResolutionContext { CardCode = "C1", ItemCode = "I1", BplId = 1 };
        var winner = await resolver.ResolveAsync(ctx, CancellationToken.None);

        winner.Id.Should().Be("S");
    }

    [Fact]
    public async Task Lanca_se_nao_houver_regra()
    {
        var resolver = Build(Array.Empty<FiscalRule>());
        var ctx = new FiscalResolutionContext { CardCode = "X", ItemCode = "Y", BplId = 1 };
        await Assert.ThrowsAsync<DomainException>(() => resolver.ResolveAsync(ctx, CancellationToken.None));
    }
}
