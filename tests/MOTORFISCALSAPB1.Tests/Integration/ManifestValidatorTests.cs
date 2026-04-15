using FluentAssertions;
using MOTORFISCALSAPB1.Integration.SapB1.Structure;
using MOTORFISCALSAPB1.Shared.Contracts;
using Xunit;

namespace MOTORFISCALSAPB1.Tests.Integration;

public class ManifestValidatorTests
{
    private readonly ManifestValidator _v = new();

    [Fact]
    public void Aceita_manifesto_valido()
    {
        var m = new StructureManifest
        {
            UserTables = new()
            {
                new() { Name = "MF_RULE", Description = "Regras", TableType = "bott_MasterData" }
            },
            UserFields = new()
            {
                new() { Table = "@MF_RULE", Name = "DESCRICAO", Description = "Descricao",
                    FieldType = "db_Alpha", SubType = "st_None", Size = 200, Mandatory = true }
            }
        };
        _v.Validate(m).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Detecta_duplicidade_de_UDF()
    {
        var m = new StructureManifest
        {
            UserFields = new()
            {
                new() { Table = "@MF_RULE", Name = "DESCRICAO", Description = "x", FieldType = "db_Alpha", SubType = "st_None" },
                new() { Table = "@MF_RULE", Name = "DESCRICAO", Description = "y", FieldType = "db_Alpha", SubType = "st_None" },
            }
        };
        _v.Validate(m).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Detecta_tipo_invalido()
    {
        var m = new StructureManifest
        {
            UserFields = new()
            {
                new() { Table = "OCRD", Name = "MF_X", Description = "x", FieldType = "db_Unknown", SubType = "st_None" }
            }
        };
        _v.Validate(m).IsValid.Should().BeFalse();
    }
}
