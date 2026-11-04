using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColInboundHeaderCorrelationTests
{
    [Theory]
    [InlineData("0001283.008.20260731.8.OUT", "H", "0001283.008.20260731.8.OUT")]
    [InlineData("0001283.009.20260731.9.OUT.env", "I", "0001283.009.20260731.9.OUT")]
    [InlineData("0001283.008.20260731.10.OUT.env", "H", "0001283.008.20260731.10.OUT")]
    [InlineData("0001283.036.20260731.123.OUT", "9", "0001283.036.20260731.123.OUT")]
    public void ValidPhysicalName_CorrelatesWithPlainHeader_IgnoresInternalCycle(
        string physical, string expectedId, string canonical)
    {
        var records = new[] { Header("20260731", expectedId[0]) };
        Assert.True(AchColInboundHeaderCorrelation.TryValidate(physical, canonical, records, out var parsed, out var error));
        Assert.Null(error);
        Assert.NotNull(parsed);
        Assert.Equal(int.Parse(physical.Split('.')[3]), parsed.ExternalCycleNumber);
    }

    [Theory]
    [InlineData("0001283.008.20260731.8.OUT", "G", "20260731", "0001283.008.20260731.8.OUT", "ACHCOL_INBOUND_FILENAME_HEADER_IDENTIFIER_MISMATCH")]
    [InlineData("0001283.009.20260731.9.OUT", "H", "20260731", "0001283.009.20260731.9.OUT", "ACHCOL_INBOUND_FILENAME_HEADER_IDENTIFIER_MISMATCH")]
    [InlineData("0001283.008.20260731.8.OUT", "H", "20260730", "0001283.008.20260731.8.OUT", "ACHCOL_INBOUND_FILENAME_HEADER_DATE_MISMATCH")]
    [InlineData("0001283.008.20260731.8.OUT.env", "H", "20260731", "0001283.008.20260731.8.OUT.env", "ACHCOL_INBOUND_DECRYPTED_FILENAME_MISMATCH")]
    [InlineData("0001283.000.20260731.8.OUT", "H", "20260731", "0001283.000.20260731.8.OUT", "ACHCOL_INBOUND_SEQUENCE_INVALID")]
    public void InvalidCorrelation_FailsClosedWithStableReason(
        string physical, string modifier, string headerDate, string canonical, string expectedError)
    {
        Assert.False(AchColInboundHeaderCorrelation.TryValidate(physical, canonical,
            new[] { Header(headerDate, modifier[0]) }, out _, out var error));
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void MissingHeader_FailsClosed()
    {
        Assert.False(AchColInboundHeaderCorrelation.TryValidate("0001283.008.20260731.8.OUT",
            "0001283.008.20260731.8.OUT", [new string('9', 106)], out _, out var error));
        Assert.Equal("ACHCOL_INBOUND_HEADER_MISSING", error);
    }

    private static string Header(string date, char identifier)
    {
        var record = Enumerable.Repeat(' ', 106).ToArray();
        record[0] = '1';
        date.CopyTo(0, record, 23, 8);
        record[35] = identifier;
        return new string(record);
    }
}
