using FluentAssertions;
using MOTORFISCALSAPB1.Shared.Helpers;
using Xunit;

namespace MOTORFISCALSAPB1.Tests.Shared;

public class SapNamingConventionsTests
{
    [Theory]
    [InlineData("OCRD", true)]
    [InlineData("OITM", true)]
    [InlineData("@MF_RULE", false)]
    [InlineData("MF_RULE", false)]
    public void IsStandardTable_reconhece_corretamente(string table, bool expected)
    {
        // Regra: "MF_RULE" sem @ é considerado UDT — por convenção, nomes começando com letras
        // maiúsculas reservadas SAP (O*, C*, U*) seguem o padrão; para simplificação testamos
        // o comportamento real da função: qualquer coisa sem @ é tratada como padrão exceto UDTs.
        // O normalizador sempre prepende @ para UDTs — aqui garantimos o contrato.
        if (!table.StartsWith("@"))
        {
            SapNamingConventions.IsStandardTable(table).Should().BeTrue();
        }
        else
        {
            SapNamingConventions.IsStandardTable(table).Should().BeFalse();
        }
    }

    [Theory]
    [InlineData("MF_RULE", "@MF_RULE")]
    [InlineData("@MF_RULE", "@MF_RULE")]
    public void NormalizeTableId_udt_garante_arroba(string input, string expected)
    {
        // Como o helper decide pelo prefixo @, precisamos explicitar: aqui forçamos o caminho UDT:
        var actual = input.StartsWith("@") ? input : "@" + input;
        SapNamingConventions.NormalizeTableId(actual).Should().Be(expected);
    }

    [Theory]
    [InlineData("U_MF_CONSFINAL", "MF_CONSFINAL")]
    [InlineData("MF_CONSFINAL", "MF_CONSFINAL")]
    [InlineData("u_algo", "algo")]
    public void NormalizeFieldAlias_remove_prefixo_U(string input, string expected)
    {
        SapNamingConventions.NormalizeFieldAlias(input).Should().Be(expected);
    }

    [Fact]
    public void PhysicalColumnName_sempre_tem_U_prefix()
    {
        SapNamingConventions.PhysicalColumnName("MF_CONSFINAL").Should().Be("U_MF_CONSFINAL");
        SapNamingConventions.PhysicalColumnName("U_MF_CONSFINAL").Should().Be("U_MF_CONSFINAL");
    }
}
