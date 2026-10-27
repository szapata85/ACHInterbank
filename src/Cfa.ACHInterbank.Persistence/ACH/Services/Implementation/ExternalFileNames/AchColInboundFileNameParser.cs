using System.Globalization;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

/// <summary>
/// Parses ACH Colombia's *physical inbound* OUT file names. The external cycle
/// number (N) is not an ACHINTERBANK AchCycle identifier and must never be used
/// as one without a separate, explicit operational resolution.
/// This parser deliberately does not alter CENIT or outbound naming policies.
/// </summary>
internal static class AchColInboundFileNameParser
{
    // ACH Colombia V35: 001-026 -> A-Z; 027-036 -> 0-9.
    internal const int MaxFileSequence = 36;
    private const int MaxPhysicalNameLength = 128;

    internal static bool TryParse(
        string? physicalFileName,
        out AchColInboundFileName? parsed,
        out string? errorCode)
    {
        parsed = null;
        errorCode = null;

        // Accept a file NAME, not a path. Never silently strip a parent directory.
        if (string.IsNullOrEmpty(physicalFileName)
            || physicalFileName.Length > MaxPhysicalNameLength
            || physicalFileName != physicalFileName.Trim()
            || physicalFileName.IndexOfAny(['/', '\\', ':']) >= 0
            || physicalFileName.Any(char.IsControl))
        {
            errorCode = "ACHCOL_INBOUND_NAME_UNSAFE";
            return false;
        }

        // RRRRTTT.ZZZ.yyyyMMdd.N.OUT[.env], with exact dot-separated tokens.
        // Unlike an extension-only check this cannot misclassify a digital envelope
        // as an unencrypted NACHA payload.
        var parts = physicalFileName.Split('.');
        if ((parts.Length != 5 && parts.Length != 6)
            || !parts[4].Equals("OUT", StringComparison.OrdinalIgnoreCase)
            || (parts.Length == 6 && !parts[5].Equals("env", StringComparison.OrdinalIgnoreCase)))
        {
            errorCode = "ACHCOL_INBOUND_NAME_FORMAT_INVALID";
            return false;
        }

        if (parts[0].Length != 7 || !IsAsciiDigits(parts[0]))
        {
            errorCode = "ACHCOL_INBOUND_ORIGIN_INVALID";
            return false;
        }

        if (parts[1].Length != 3
            || !IsAsciiDigits(parts[1])
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            || sequence is < 1 or > MaxFileSequence)
        {
            errorCode = "ACHCOL_INBOUND_SEQUENCE_INVALID";
            return false;
        }

        if (parts[2].Length != 8
            || !IsAsciiDigits(parts[2])
            || !DateOnly.TryParseExact(parts[2], "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fileDate))
        {
            errorCode = "ACHCOL_INBOUND_DATE_INVALID";
            return false;
        }

        // N is independent from ZZZ and has no arbitrary 'seven cycles' ceiling.
        // Reject zero, leading zeros, signs, overflow and non-ASCII digits.
        if (parts[3].Length == 0
            || parts[3][0] is < '1' or > '9'
            || !IsAsciiDigits(parts[3])
            || !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var externalCycle))
        {
            errorCode = "ACHCOL_INBOUND_EXTERNAL_CYCLE_INVALID";
            return false;
        }

        var isEncrypted = parts.Length == 6;
        parsed = new AchColInboundFileName(
            physicalFileName,
            isEncrypted ? physicalFileName[..^4] : physicalFileName,
            parts[0],
            sequence,
            fileDate,
            externalCycle,
            isEncrypted ? AchColInboundProtectionKind.DigitalEnvelope : AchColInboundProtectionKind.PlainNacha);
        return true;
    }

    private static bool IsAsciiDigits(string token)
        => token.All(c => c is >= '0' and <= '9');

    internal enum AchColInboundProtectionKind
    {
        PlainNacha,
        DigitalEnvelope
    }

    internal sealed record AchColInboundFileName(
        string OriginalPhysicalFileName,
        string CanonicalPlainFileName,
        string OriginAndTransitCode,
        int FileSequence,
        DateOnly FileDate,
        int ExternalCycleNumber,
        AchColInboundProtectionKind ProtectionKind)
    {
        internal bool RequiresDecryption => ProtectionKind == AchColInboundProtectionKind.DigitalEnvelope;

        // The identifier comes from the official 001–036 sequence mapping, not N.
        internal char FileIdModifier => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[FileSequence - 1];
    }
}
