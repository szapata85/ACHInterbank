using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColInboundFileNameParserTests
{
    [Theory]
    [InlineData("0001283.008.20260731.8.OUT", 8, 8, false, "0001283.008.20260731.8.OUT", "2026-07-31")]
    [InlineData("0001283.009.20260731.9.OUT.env", 9, 9, true, "0001283.009.20260731.9.OUT", "2026-07-31")]
    [InlineData("0001283.008.20260731.10.OUT.env", 8, 10, true, "0001283.008.20260731.10.OUT", "2026-07-31")]
    [InlineData("0001283.036.20260228.123.OUT", 36, 123, false, "0001283.036.20260228.123.OUT", "2026-02-28")]
    [InlineData("0001283.001.20260228.1.out.ENV", 1, 1, true, "0001283.001.20260228.1.out", "2026-02-28")]
    [InlineData("0001283.026.20260101.2147483647.out", 26, int.MaxValue, false, "0001283.026.20260101.2147483647.out", "2026-01-01")]
    public void ValidPhysicalFileName_ParsesIndependentSequenceCycleAndProtection(
        string input, int expectedSequence, int expectedExternalCycle, bool encrypted, string expectedCanonical, string expectedDate)
    {
        Assert.True(AchColInboundFileNameParser.TryParse(input, out var parsed, out var errorCode));
        Assert.Null(errorCode);
        Assert.NotNull(parsed);
        Assert.Equal(input, parsed.OriginalPhysicalFileName);
        Assert.Equal(expectedCanonical, parsed.CanonicalPlainFileName);
        Assert.Equal("0001283", parsed.OriginAndTransitCode);
        Assert.Equal(expectedSequence, parsed.FileSequence);
        Assert.Equal(expectedExternalCycle, parsed.ExternalCycleNumber);
        Assert.Equal(DateOnly.ParseExact(expectedDate, "yyyy-MM-dd"), parsed.FileDate);
        Assert.Equal(encrypted, parsed.RequiresDecryption);
        Assert.Equal(encrypted
                ? AchColInboundFileNameParser.AchColInboundProtectionKind.DigitalEnvelope
                : AchColInboundFileNameParser.AchColInboundProtectionKind.PlainNacha,
            parsed.ProtectionKind);
    }

    [Theory]
    [InlineData(null, "ACHCOL_INBOUND_NAME_UNSAFE")]
    [InlineData("", "ACHCOL_INBOUND_NAME_UNSAFE")]
    [InlineData(" 0001283.008.20260731.8.OUT", "ACHCOL_INBOUND_NAME_UNSAFE")]
    [InlineData("/tmp/0001283.008.20260731.8.OUT", "ACHCOL_INBOUND_NAME_UNSAFE")]
    [InlineData("C:\\inbound\\0001283.008.20260731.8.OUT", "ACHCOL_INBOUND_NAME_UNSAFE")]
    [InlineData("0001283.008.20260731.8.OUT\n", "ACHCOL_INBOUND_NAME_UNSAFE")]
    [InlineData("0001283.008.20260731.8.OUT.tmp", "ACHCOL_INBOUND_NAME_FORMAT_INVALID")]
    [InlineData("0001283.008.20260731.8.OUT.env.tmp", "ACHCOL_INBOUND_NAME_FORMAT_INVALID")]
    [InlineData("0001283.008.20260731.8.RET.env", "ACHCOL_INBOUND_NAME_FORMAT_INVALID")]
    [InlineData("0001283.008.20260731.8.OUT.env.env", "ACHCOL_INBOUND_NAME_FORMAT_INVALID")]
    [InlineData("0001283.008.20260731.8.OUT.en", "ACHCOL_INBOUND_NAME_FORMAT_INVALID")]
    [InlineData("0001283.008.20260731.8", "ACHCOL_INBOUND_NAME_FORMAT_INVALID")]
    [InlineData("000128.008.20260731.8.OUT", "ACHCOL_INBOUND_ORIGIN_INVALID")]
    [InlineData("000128X.008.20260731.8.OUT", "ACHCOL_INBOUND_ORIGIN_INVALID")]
    [InlineData("0001283.000.20260731.8.OUT", "ACHCOL_INBOUND_SEQUENCE_INVALID")]
    [InlineData("0001283.037.20260731.8.OUT", "ACHCOL_INBOUND_SEQUENCE_INVALID")]
    [InlineData("0001283.08.20260731.8.OUT", "ACHCOL_INBOUND_SEQUENCE_INVALID")]
    [InlineData("0001283.008.20260229.8.OUT", "ACHCOL_INBOUND_DATE_INVALID")]
    [InlineData("0001283.008.20261301.8.OUT", "ACHCOL_INBOUND_DATE_INVALID")]
    [InlineData("0001283.008.20260731.0.OUT", "ACHCOL_INBOUND_EXTERNAL_CYCLE_INVALID")]
    [InlineData("0001283.008.20260731.08.OUT", "ACHCOL_INBOUND_EXTERNAL_CYCLE_INVALID")]
    [InlineData("0001283.008.20260731.-8.OUT", "ACHCOL_INBOUND_EXTERNAL_CYCLE_INVALID")]
    [InlineData("0001283.008.20260731.2147483648.OUT", "ACHCOL_INBOUND_EXTERNAL_CYCLE_INVALID")]
    public void InvalidPhysicalFileName_FailsClosedWithStableError(string? input, string expectedError)
    {
        Assert.False(AchColInboundFileNameParser.TryParse(input, out var parsed, out var error));
        Assert.Null(parsed);
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void UnencryptedAndEncryptedForms_AgreeOnOperationalMetadata_WithoutInferringAnInternalCycle()
    {
        Assert.True(AchColInboundFileNameParser.TryParse("0001283.008.20260731.10.OUT", out var plain, out _));
        Assert.True(AchColInboundFileNameParser.TryParse("0001283.008.20260731.10.OUT.env", out var encrypted, out _));
        Assert.NotNull(plain);
        Assert.NotNull(encrypted);
        Assert.Equal(plain.CanonicalPlainFileName, encrypted.CanonicalPlainFileName);
        Assert.Equal(plain.FileSequence, encrypted.FileSequence);
        Assert.Equal(plain.ExternalCycleNumber, encrypted.ExternalCycleNumber);
        Assert.False(plain.RequiresDecryption);
        Assert.True(encrypted.RequiresDecryption);
        // The parsed contract deliberately has NO AchCycleId property.
    }

    [Fact]
    public void OtherChamberNaming_IsNotAcceptedByAchColParser()
    {
        Assert.False(AchColInboundFileNameParser.TryParse("0001283.002.20260713.1", out _, out _));
        Assert.True(CenitOfficialFileNameParser.TryParseCenitFileName("0001283.002.20260713.1", out _));
    }
}
