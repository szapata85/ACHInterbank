using System.Globalization;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

/// <summary>
/// Confirms that a physical ACH Colombia OUT filename matches its decrypted/plain
/// NACHA-M type-1 header. Does not infer an internal operational AchCycle.
/// This must run AFTER the envelope has been decrypted, never on ciphertext.
/// </summary>
internal static class AchColInboundHeaderCorrelation
{
    private const string FileIdentifierSymbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    internal static bool TryValidate(
        string physicalFileName,
        string canonicalPlainFileName,
        IReadOnlyList<string> records,
        out AchColInboundFileNameParser.AchColInboundFileName? parsed,
        out string? errorCode)
    {
        if (!AchColInboundFileNameParser.TryParse(physicalFileName, out parsed, out errorCode))
            return false;

        if (!string.Equals(parsed!.CanonicalPlainFileName, canonicalPlainFileName, StringComparison.OrdinalIgnoreCase))
        {
            errorCode = "ACHCOL_INBOUND_DECRYPTED_FILENAME_MISMATCH";
            return false;
        }

        var header = records.FirstOrDefault(record => record.Length == 106 && record[0] == '1');
        if (header is null)
        {
            errorCode = "ACHCOL_INBOUND_HEADER_MISSING";
            return false;
        }

        // 1-based NACHA fields: file creation date 24-31, identifier 36.
        var dateToken = header.Substring(23, 8);
        if (!DateOnly.TryParseExact(dateToken, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var headerDate)
            || headerDate != parsed.FileDate)
        {
            errorCode = "ACHCOL_INBOUND_FILENAME_HEADER_DATE_MISMATCH";
            return false;
        }

        var expectedModifier = FileIdentifierSymbols[parsed.FileSequence - 1];
        if (header[35] != expectedModifier)
        {
            errorCode = "ACHCOL_INBOUND_FILENAME_HEADER_IDENTIFIER_MISMATCH";
            return false;
        }

        // Invariant: parsed.ExternalCycleNumber is NEVER compared to an AchCycleId.
        errorCode = null;
        return true;
    }
}
