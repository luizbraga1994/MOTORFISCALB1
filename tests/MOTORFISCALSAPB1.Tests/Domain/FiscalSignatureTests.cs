using FluentAssertions;
using MOTORFISCALSAPB1.Domain.Fiscal;
using Xunit;

namespace MOTORFISCALSAPB1.Tests.Domain;

public class FiscalSignatureTests
{
    private static FiscalSignature Build(decimal icms) => new(
        cfop: "5102",
        cstIcms: "00",
        aliquotaIcms: icms,
        cstIpi: "99",
        aliquotaIpi: 0m,
        cstPis: "01",
        aliquotaPis: 1.65m,
        cstCofins: "01",
        aliquotaCofins: 7.6m,
        temSt: false,
        aliquotaStInterna: null,
        mvaSt: null);

    [Fact]
    public void Deve_ser_deterministica()
    {
        var a = Build(18m);
        var b = Build(18m);
        a.Hash.Should().Be(b.Hash);
        a.Canonical.Should().Be(b.Canonical);
    }

    [Fact]
    public void Aliquotas_diferentes_geram_hashes_diferentes()
    {
        Build(18m).Hash.Should().NotBe(Build(12m).Hash);
    }

    [Fact]
    public void Igualdade_de_valor_por_hash()
    {
        Build(18m).Equals(Build(18m)).Should().BeTrue();
    }
}
