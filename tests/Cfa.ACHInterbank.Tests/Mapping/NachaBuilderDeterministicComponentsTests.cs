using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Mapping;

public class NachaBuilderDeterministicComponentsTests
{
    [Fact]
    public void FieldFormatter_ShouldResolveQualifiedAndNormalizedDictionaryIdentifiers()
    {
        var values = new Dictionary<string, object?>
        {
            ["Receiving_DFI"] = "12345678"
        };

        var found = NachaDeterministicFieldFormatter.TryResolveValue(values, "Entry.ReceivingDFI", out var raw);

        found.Should().BeTrue();
        raw.Should().Be("12345678");
        NachaDeterministicFieldFormatter.NormalizeIdentifier(" entry.receiving_dfi ").Should().Be("ENTRYRECEIVINGDFI");
    }

    [Fact]
    public void FieldFormatter_ShouldPreserveLegacyPrimitiveFormatting()
    {
        var field = new NachaRecordField { Format = "yyMMdd" };

        NachaDeterministicFieldFormatter.FormatLegacyValue(new DateTime(2026, 10, 10), field).Should().Be("261010");
        NachaDeterministicFieldFormatter.FormatLegacyValue(12.34m, field).Should().Be("1234");
        NachaDeterministicFieldFormatter.FormatLegacyValue(true, field).Should().Be("1");
        NachaDeterministicFieldFormatter.FormatLegacyValue(null, field).Should().BeEmpty();
    }

    [Fact]
    public void FileBlocking_ShouldCalculateBlockAndPaddingCountsDeterministically()
    {
        var result = NachaFileBlocking.Calculate(11, 10);

        result.BlockCount.Should().Be(2);
        result.PaddingRecordCount.Should().Be(9);
        result.PhysicalRecordCountAfterPadding.Should().Be(20);
    }

    [Fact]
    public void FileBlocking_ShouldResolveConfiguredBlockSizeAndPaddingRecord()
    {
        var layout = new CfgLayoutVariant
        {
            TotalLength = 106,
            Fields =
            [
                new CfgLayoutField
                {
                    FieldCode = "BLOCKINGFACTOR",
                    IsEnabled = true,
                    SourceDefinition = new CfgFieldSourceDefinition { ConstantValue = "10" }
                }
            ]
        };

        NachaFileBlocking.ResolveConfiguredBlockSize(layout).Should().Be(10);
        NachaFileBlocking.BuildPaddingRecord(layout.TotalLength).Should().Be(new string('9', 106));
    }

    [Fact]
    public void FileBlocking_ShouldFailClosedForInvalidConfiguredBlockSize()
    {
        var layout = new CfgLayoutVariant
        {
            Fields =
            [
                new CfgLayoutField
                {
                    FieldCode = "BLOCKINGFACTOR",
                    IsEnabled = true,
                    SourceDefinition = new CfgFieldSourceDefinition { ConstantValue = "0" }
                }
            ]
        };

        var act = () => NachaFileBlocking.ResolveConfiguredBlockSize(layout);

        act.Should().Throw<NachaGenerationException>()
            .Which.Code.Should().Be("NACHA_BLOCK_SIZE_INVALID");
    }
}
