using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

internal static class CenitOfficialFileNameParser
{
    private static readonly Regex CenitOfficialFileNameRegex = new(
        @"^(?<origin>\d{7})\.(?<cycle>\d{3})\.(?<date>\d{8})\.(?<suffix>[1-9]\d*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static bool TryParseCenitFileName(
        string? fileName,
        out CenitOfficialFileName? parsed)
    {
        parsed = null;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var name = Path.GetFileName(fileName.Trim());
        var match = CenitOfficialFileNameRegex.Match(name);

        if (!match.Success
            || !int.TryParse(match.Groups["cycle"].Value, out var cycle)
            || !DateOnly.TryParseExact(
                match.Groups["date"].Value,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date)
            || !int.TryParse(match.Groups["suffix"].Value, out var suffix))
        {
            return false;
        }

        parsed = new CenitOfficialFileName(
            match.Groups["origin"].Value,
            cycle,
            date,
            suffix);

        return true;
    }

    internal static int? ExtractCycleNumberFromFileName(string fileName)
        => TryParseCenitFileName(fileName, out var parsed)
            ? parsed!.CycleNumber
            : null;

    internal sealed record CenitOfficialFileName(
        string OriginCode,
        int CycleNumber,
        DateOnly FileDate,
        int Suffix);
}
