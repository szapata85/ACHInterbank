using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitOfficialFileNameParserTests
{
    [Theory]
    [InlineData("0001283.002.20260713.1", 2, 1)]
    [InlineData("0001283.005.20260713.1", 5, 1)]
    [InlineData("0001283.002.20260713.9", 2, 9)]
    public void TryParseCenitFileName_ShouldReturnCycleDateAndSuffix(
        string fileName,
        int expectedCycleNumber,
        int expectedSuffix)
    {
        var parsed = AssertParse(fileName);

        Assert.Equal("0001283", parsed.OriginCode);
        Assert.Equal(expectedCycleNumber, parsed.CycleNumber);
        Assert.Equal(new DateOnly(2026, 7, 13), parsed.FileDate);
        Assert.Equal(expectedSuffix, parsed.Suffix);
    }

    [Theory]
    [InlineData("0001283.002.20260713.1.ach")]
    [InlineData("0001283.20260713.002.1")]
    public void TryParseCenitFileName_ShouldRejectInvalidNames(string fileName)
    {
        Assert.False(CenitOfficialFileNameParser.TryParseCenitFileName(fileName, out var parsed));
        Assert.Null(parsed);
        Assert.Null(CenitOfficialFileNameParser.ExtractCycleNumberFromFileName(fileName));
    }

    [Fact]
    public void FourthSegment_ShouldRemainSuffix_NotCycleNumber()
    {
        var parsed = AssertParse("0001283.002.20260713.9");

        Assert.Equal(2, parsed.CycleNumber);
        Assert.Equal(9, parsed.Suffix);
    }

    private static CenitOfficialFileNameParser.CenitOfficialFileName AssertParse(string fileName)
    {
        var parsed = AssertParseNullable(fileName);
        Assert.NotNull(parsed);
        return parsed!;
    }

    private static CenitOfficialFileNameParser.CenitOfficialFileName? AssertParseNullable(string fileName)
    {
        Assert.True(CenitOfficialFileNameParser.TryParseCenitFileName(fileName, out var parsed));
        return parsed;
    }
}
