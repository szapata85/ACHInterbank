using System.Text;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaPhysicalParserTests
{
    [Fact]
    public async Task ParseAsync_AchColombiaHappyPath_ShouldTokenizeFixedWidthRecordsWithoutDbContext()
    {
        var content = BuildTenRecordFile('7', "05ACH PHYSICAL CHARACTERIZATION");
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));

        var parsed = await NachaPhysicalParser.ParseAsync(stream, CancellationToken.None);

        parsed.RecordLength.Should().Be(106);
        parsed.Records.Should().HaveCount(10);
        parsed.Records.Should().OnlyContain(record => record.Content.Length == 106);
        parsed.Records.Take(6).Select(record => record.RecordType)
            .Should().Equal('1', '5', '6', '7', '8', '9');
        parsed.Records.Select(record => record.Index).Should().Equal(Enumerable.Range(0, 10));
        parsed.Records[3].Content.Substring(1, 2).Should().Be("05");
    }

    [Fact]
    public async Task ParseAsync_CenitReturnOfReturnFixture_ShouldPreservePhysicalAddendaAndOrder()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Nacha",
            "NormativeSynthetic",
            "Cenit",
            "Ror",
            "CENIT_ROR_IN_001.ach");
        var content = string.Concat(File.ReadAllLines(fixturePath, Encoding.Latin1));
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));

        var parsed = await NachaPhysicalParser.ParseAsync(stream, CancellationToken.None);

        parsed.Records.Should().HaveCount(10);
        parsed.Records.Take(6).Select(record => record.RecordType)
            .Should().Equal('1', '5', '6', '7', '8', '9');
        var addenda = parsed.Records[3].Content;
        addenda.Substring(1, 2).Should().Be("99");
        addenda.Substring(3, 3).Should().Be("R61");
        addenda.Substring(6, 15).Should().Be("223456780003312");
        addenda.Substring(81, 15).Should().Be("876543259602872");
    }

    [Theory]
    [InlineData("LINE_ENDING", "ACHCOL-PHYSICAL-NO-LINE-ENDINGS")]
    [InlineData("RESIDUAL_BYTE", "ACHCOL-PHYSICAL-RECORD-LENGTH")]
    [InlineData("BAD_FIRST_RECORD", "ACHCOL-T1-RECORD-TYPE")]
    [InlineData("BAD_BLOCKING", "ACHCOL-PHYSICAL-BLOCKING-FACTOR")]
    public async Task ParseAsync_InvalidPhysicalStructure_ShouldPreserveExistingFailureCodes(
        string mutation,
        string expectedCode)
    {
        var content = BuildTenRecordFile('7', "05INVALID STRUCTURE CHARACTERIZE");
        byte[] bytes = mutation switch
        {
            "LINE_ENDING" => Encoding.Latin1.GetBytes(content + "\r\n"),
            "RESIDUAL_BYTE" => Encoding.Latin1.GetBytes(content + "X"),
            "BAD_FIRST_RECORD" => Encoding.Latin1.GetBytes("5" + content[1..]),
            "BAD_BLOCKING" => Encoding.Latin1.GetBytes(Replace(content, 39, "09")),
            _ => throw new InvalidOperationException(mutation)
        };
        await using var stream = new MemoryStream(bytes);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NachaPhysicalParser.ParseAsync(stream, CancellationToken.None));

        exception.Message.Should().Contain(expectedCode);
    }

    [Theory]
    [InlineData(0xD1, 'Ñ')]
    [InlineData(0xF1, 'ñ')]
    public async Task ParseAsync_NormativeLatin1Enye_ShouldRemainAccepted(int encodedValue, char expected)
    {
        var content = BuildTenRecordFile('7', "05LATIN1 CHARACTERIZATION");
        var bytes = Encoding.Latin1.GetBytes(content);
        bytes[(2 * 106) + 62] = (byte)encodedValue;
        await using var stream = new MemoryStream(bytes);

        var parsed = await NachaPhysicalParser.ParseAsync(stream, CancellationToken.None);

        parsed.Records[2].Content[62].Should().Be(expected);
    }

    private static string BuildTenRecordFile(char addendaRecordType, string addendaPayload)
    {
        var type1 = Record('1');
        type1 = Replace(type1, 36, "106");
        type1 = Replace(type1, 39, "10");

        var type5 = Record('5');
        var type6 = Record('6');
        var type7 = Record(addendaRecordType);
        type7 = Replace(type7, 1, addendaPayload.PadRight(105)[..105]);
        var type8 = Record('8');
        var type9 = Record('9');
        var padding = new string('9', 106);

        return string.Concat(type1, type5, type6, type7, type8, type9, padding, padding, padding, padding);
    }

    private static string Record(char recordType)
        => recordType + new string(' ', 105);

    private static string Replace(string source, int zeroBasedStart, string value)
    {
        var chars = source.ToCharArray();
        value.CopyTo(0, chars, zeroBasedStart, value.Length);
        return new string(chars);
    }
}
