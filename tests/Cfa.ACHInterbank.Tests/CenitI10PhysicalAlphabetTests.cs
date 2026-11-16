using System.Text;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I10 / PT-11 / PT-20: synthetic fixed-width byte fixtures; no database, network or operator assumed.
/// </summary>
public sealed class CenitI10PhysicalAlphabetTests
{
    private const int PhysicalRecordLength = 106;
    private const int TestOffset = (2 * PhysicalRecordLength) + 62;
    private const string CenitSpecials = " .,:;-*/&#$%=";

    [Fact]
    public async Task Cenit_CompleteSingleByteMatrix_MustEqualPublished2026Repertoire()
    {
        var source = BuildTenRecordFile();
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var bytes = Encoding.Latin1.GetBytes(source);
            bytes[TestOffset] = (byte)value;
            await using var stream = new MemoryStream(bytes);
            var valid = IsDocumentedCenitByte((byte)value);

            if (valid)
            {
                var parsed = await NachaPhysicalParser.ParseAsync(stream, CancellationToken.None,
                    isCenitNachaProfile: true);
                parsed.Records.Should().HaveCount(10);
                parsed.Records[2].Content[62].Should().Be((char)value);
            }
            else
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    NachaPhysicalParser.ParseAsync(stream, CancellationToken.None,
                        isCenitNachaProfile: true));
                error.Message.Should().Contain("CENIT-NACHA-FATAL").And.NotContain("ACHCOL-");
            }
        }
    }

    [Fact]
    public async Task AchColombia_SamePhysicalFixture_RetainsLatin1EnyeWithoutBroadeningOtherHighBytes()
    {
        var original = BuildTenRecordFile();
        foreach (var value in new byte[] { 0xD1, 0xF1 })
        {
            var bytes = Encoding.Latin1.GetBytes(original);
            bytes[TestOffset] = value;
            await using var stream = new MemoryStream(bytes);
            var parsed = await NachaPhysicalParser.ParseAsync(stream, CancellationToken.None);
            parsed.Records[2].Content[62].Should().Be((char)value);
        }

        foreach (var value in new byte[] { 0xE1, 0xC3, 0x7F, (byte)'?', (byte)'|' })
        {
            var bytes = Encoding.Latin1.GetBytes(original);
            bytes[TestOffset] = value;
            await using var stream = new MemoryStream(bytes);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                NachaPhysicalParser.ParseAsync(stream, CancellationToken.None));
            error.Message.Should().Contain("ACHCOL-PHYSICAL-ASCII-REPERTOIRE");
        }
    }

    [Theory]
    [InlineData('Ñ')]
    [InlineData('ñ')]
    [InlineData('á')]
    [InlineData('é')]
    [InlineData('?')]
    [InlineData('|')]
    [InlineData('\\')]
    [InlineData('\t')]
    public void CenitGeneratedContent_MustFailBeforeAsciiReplacementAndHash(char invalid)
    {
        var content = BuildTenRecordFile();
        var withInvalid = content[..TestOffset] + invalid + content[(TestOffset + 1)..];
        var action = () => NachaFileGenerationCoordinator.ValidateCenitOutputAlphabet("CENIT", withInvalid);
        action.Should().Throw<NachaGenerationException>()
            .Which.Code.Should().Be("CENIT_NACHA_CHARACTER_REPERTOIRE_INVALID");
    }

    [Fact]
    public void GeneratedCenit_FullPublishedPunctuationAndCase_MustRemainUnchanged()
    {
        var original = BuildTenRecordFile();
        foreach (var character in "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" + CenitSpecials)
        {
            var content = original[..TestOffset] + character + original[(TestOffset + 1)..];
            NachaFileGenerationCoordinator.ValidateCenitOutputAlphabet("CENIT", content);
            NachaPhysicalParser.IsCenitAcceptedCharacter(character).Should().BeTrue();
        }
    }

    [Fact]
    public void AchColombiaGeneration_IsUnchangedByCenitSpecificGate()
    {
        var content = BuildTenRecordFile();
        foreach (var invalidForCenit in new[] { 'Ñ', 'ñ', 'á' })
        {
            var scenario = content[..TestOffset] + invalidForCenit + content[(TestOffset + 1)..];
            // This check is only the NEW CENIT gate. ACH Colombia's own generator rules still apply.
            NachaFileGenerationCoordinator.ValidateCenitOutputAlphabet("ACH", scenario);
        }
    }

    [Fact]
    public async Task ExistingCenitRorGoldenFixture_MustPassPhysicalAlphabetAndLayout()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "TestData", "Nacha",
            "NormativeSynthetic", "Cenit", "Ror", "CENIT_ROR_IN_001.ach");
        var content = string.Concat(File.ReadAllLines(file, Encoding.Latin1));
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));
        var parsed = await NachaPhysicalParser.ParseAsync(stream, CancellationToken.None,
            isCenitNachaProfile: true);
        parsed.RecordLength.Should().Be(106);
        parsed.Records.Should().HaveCount(10);
        NachaFileGenerationCoordinator.ValidateCenitOutputAlphabet("CENIT", content);
    }

    private static bool IsDocumentedCenitByte(byte value)
        => value is >= (byte)'0' and <= (byte)'9'
            or >= (byte)'A' and <= (byte)'Z'
            or >= (byte)'a' and <= (byte)'z'
            || CenitSpecials.Contains((char)value);

    private static string BuildTenRecordFile()
    {
        var header = new char[PhysicalRecordLength];
        Array.Fill(header, ' ');
        header[0] = '1';
        "106".CopyTo(0, header, 36, 3);
        "10".CopyTo(0, header, 39, 2);
        string Record(char type) => type + new string(' ', PhysicalRecordLength - 1);
        return new string(header) + Record('5') + Record('6') + Record('7')
            + Record('8') + Record('9') + string.Concat(Enumerable.Repeat(new string('9', 106), 4));
    }
}
