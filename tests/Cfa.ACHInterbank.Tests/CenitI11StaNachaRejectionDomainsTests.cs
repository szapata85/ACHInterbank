using System.Text;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitI11StaNachaRejectionDomainsTests
{
    private const int CenitId = 11;
    private const int AchColId = 22;
    private const string StaSource = "CENIT DSP-152 Anexo B; MATRIZ_REGLAS_CENIT";

    private static AchFileRejectionCode Row(
        string code, int? houseId, string source, string description, bool active = true)
        => new()
        {
            Code = code, ClearingHouseId = houseId, RegulatorySource = source,
            Description = description, IsActive = active
        };

    [Theory]
    [InlineData("D01")]
    [InlineData("D02")]
    [InlineData("D03")]
    [InlineData("D04")]
    [InlineData("D05")]
    [InlineData("D06")]
    public void StaCodes_AreResolvedOnlyByStaBoundary(string code)
    {
        var rows = new[] { Row(code, CenitId, StaSource, $"Causal STA {code}") };

        var staCatalogue = NachaInboundRejectionCatalogSelector.SelectForSta(rows, CenitId);
        staCatalogue.Should().ContainKey(code);
        staCatalogue[code].Description.Should().Be($"Causal STA {code}");
        NachaInboundRejectionCatalogSelector.SelectForNacha(rows, CenitId, true)
            .Should().BeEmpty();
        NachaInboundRejectionCatalogSelector.SelectForNacha(rows, AchColId, false)
            .Should().BeEmpty();
    }

    [Fact]
    public void D02StaProtection_AndNachaPhysicalError_CannotBeConfused()
    {
        var rows = new[]
        {
            Row("D02", CenitId, StaSource, "STA: archivo firmado y cifrado para otro operador"),
            Row("D02", AchColId, "ACH Colombia catálogo NACHA", "ACH: rechazo por bloques"),
        };

        NachaInboundRejectionCatalogSelector.SelectForSta(rows, CenitId)["D02"]
            .Description.Should().Contain("STA:");
        var cenitCatalog = NachaInboundRejectionCatalogSelector.SelectForNacha(rows, CenitId, true);
        var validator = new NachaInboundValidator(cenitCatalog);
        validator.SelectNachaErrorDomain(true);
        var action = () => validator.ThrowRegulatory("D02", "Bloque NACHA inválido.");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*[Regulatory]*CENIT-NACHA-FATAL: Bloque NACHA inválido.*")
            .Which.Message.Should().NotContain("D02").And.NotContain("firmado");
    }

    [Fact]
    public void AchColombia_HomonymousCode_UsesOnlyAchColombiaDescription()
    {
        var rows = new[]
        {
            Row("D04", CenitId, StaSource, "STA: archivo duplicado"),
            Row("D04", AchColId, "ACH Colombia NACHA", "ACH: secuencia duplicada"),
            Row("I500", null, "Técnico", "Error genérico")
        };
        var catalog = NachaInboundRejectionCatalogSelector.SelectForNacha(rows, AchColId, false);
        catalog.Should().ContainKey("D04");
        catalog.Should().ContainKey("I500");
        catalog["D04"].Description.Should().Be("ACH: secuencia duplicada");
        var validator = new NachaInboundValidator(catalog);
        validator.SelectNachaErrorDomain(false);
        var action = () => validator.ThrowRegulatory("D04", "Fallo local.");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*[Regulatory]*D04: El valor de la transacción de prenotificación es diferente de cero. Fallo local.*");
    }

    [Fact]
    public void UnknownChamber_DoesNotAdmitScopedCenitStaRejections()
    {
        var rows = new[]
        {
            Row("D02", CenitId, StaSource, "STA only"),
            Row("I500", null, "Técnico", "Error genérico")
        };
        var catalog = NachaInboundRejectionCatalogSelector.SelectForNacha(rows, null, false);
        catalog.Should().ContainKey("I500");
        catalog.Should().NotContainKey("D02");
    }

    [Fact]
    public void ChamberOwnedCode_WinsOverLegacyGenericCode()
    {
        var rows = new[]
        {
            Row("D04", null, "ACH histórico", "Código global"),
            Row("D04", AchColId, "ACH Colombia", "Código de cámara"),
        };
        var catalog = NachaInboundRejectionCatalogSelector.SelectForNacha(rows, AchColId, false);
        catalog["D04"].Description.Should().Be("Código de cámara");
    }

    [Fact]
    public void DuplicateActiveCodeInSameDomain_FailsClosed()
    {
        var rows = new[]
        {
            Row("D04", AchColId, "ACH", "Primer significado"),
            Row("D04", AchColId, "ACH", "Segundo significado"),
        };
        var action = () => NachaInboundRejectionCatalogSelector.SelectForNacha(rows, AchColId, false);
        action.Should().Throw<InvalidOperationException>().WithMessage("NACHA_REJECTION_CATALOG_AMBIGUOUS");
    }

    [Theory]
    [InlineData("CENIT", false)]
    [InlineData("ACHCOL", true)]
    [InlineData("ACH", true)]
    public void ProfileChamberMismatch_FailsBeforeParsing(string chamber, bool cenitProfile)
    {
        var action = () => NachaInboundRejectionCatalogSelector.EnsureNachaProfileMatchesChamber(123, chamber, cenitProfile);
        action.Should().Throw<InvalidOperationException>().WithMessage("NACHA_CLEARING_HOUSE_PROFILE_MISMATCH");
    }

    [Fact]
    public void MissingResolvedChamber_FailsClosed()
    {
        var action = () => NachaInboundRejectionCatalogSelector.EnsureNachaProfileMatchesChamber(123, null, false);
        action.Should().Throw<InvalidOperationException>().WithMessage("NACHA_CLEARING_HOUSE_NOT_FOUND");
    }

    [Fact]
    public async Task CenitInvalidPhysicalNacha_ReportsTechnicalErrorNotStaD02()
    {
        // Single incomplete NACHA record: never interpret as the STA encryption cause.
        using var input = new MemoryStream(Encoding.ASCII.GetBytes("1" + new string(' ', 105)));
        var action = async () => await NachaPhysicalParser.ParseAsync(
            input, CancellationToken.None, isCenitNachaProfile: true);
        var error = await action.Should().ThrowAsync<InvalidOperationException>();
        error.Which.Message.Should().Contain("[Technical]")
            .And.Contain("CENIT-NACHA-FATAL")
            .And.NotContain("D02")
            .And.NotContain("STA");
    }

    [Fact]
    public async Task ValidCenitNachaGolden_PhysicalParsingDoesNotRequireStaCatalogue()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "TestData", "Nacha",
            "NormativeSynthetic", "Cenit", "Ror", "CENIT_ROR_IN_001.ach");
        var content = string.Concat(File.ReadAllLines(fixture, Encoding.Latin1));
        using var input = new MemoryStream(Encoding.Latin1.GetBytes(content));
        var physical = await NachaPhysicalParser.ParseAsync(input, CancellationToken.None,
            isCenitNachaProfile: true);
        physical.RecordLength.Should().Be(106);
        physical.Records.Should().HaveCount(10);
        NachaInboundRejectionCatalogSelector.SelectForNacha(
            new[] { Row("D02", CenitId, StaSource, "STA D02") }, CenitId, true)
            .Should().BeEmpty();
    }

    [Fact]
    public void InactiveStaRows_CannotBeReadAsActiveStaCauses()
    {
        var rows = new[] { Row("D02", CenitId, StaSource, "Deshabilitada", active: false) };
        NachaInboundRejectionCatalogSelector.SelectForSta(rows, CenitId).Should().BeEmpty();
    }
}
