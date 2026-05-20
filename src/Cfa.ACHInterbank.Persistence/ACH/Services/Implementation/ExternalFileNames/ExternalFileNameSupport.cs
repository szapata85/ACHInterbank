using System.Globalization;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

internal static class ExternalFileNameSupport
{
    private static readonly Regex AchRegex = new(@"^(?<route>\d{4})(?<transit>\d{3})\.(?<seq>\d{3})\.1$", RegexOptions.Compiled);

    public static ExternalFileNameComponents Parse(ExternalFileNameContext context, string externalFileName)
    {
        if (IsAch(context))
        {
            var match = AchRegex.Match(externalFileName);
            if (!match.Success)
            {
                return new ExternalFileNameComponents { FullName = externalFileName };
            }

            var sequence = int.Parse(match.Groups["seq"].Value, CultureInfo.InvariantCulture);
            return new ExternalFileNameComponents
            {
                FullName = externalFileName,
                Prefix = $"{match.Groups["route"].Value}{match.Groups["transit"].Value}",
                ExternalSequence = sequence
            };
        }

        if (IsStaReject(context))
        {
            var pieces = externalFileName.Split('.');
            if (pieces.Length >= 3 && int.TryParse(pieces[2], out var detailCount))
            {
                return new ExternalFileNameComponents
                {
                    FullName = externalFileName,
                    Prefix = pieces[0],
                    DeclaredDetailCount = detailCount
                };
            }
        }

        return new ExternalFileNameComponents { FullName = externalFileName };
    }

    public static bool IsAch(ExternalFileNameContext context) =>
        context.ExternalFileType == ExternalFileType.NachaOut
        && context.Direction == ExternalFileDirection.Outbound;

    public static bool IsCenit(ExternalFileNameContext context) =>
        string.Equals(context.ClearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase);

    public static bool IsStaReject(ExternalFileNameContext context) =>
        IsCenit(context) && context.ExternalFileType == ExternalFileType.StaReject;

    public static bool IsUnconfirmedReturnLikeOutFlow(ExternalFileNameContext context) =>
        context.Direction == ExternalFileDirection.Outbound
        && context.ExternalFileType is ExternalFileType.ReturnOut
            or ExternalFileType.ReturnOfReturnOut
            or ExternalFileType.OperatorReturnOut
            or ExternalFileType.ResponseOut
            or ExternalFileType.RejectionOut;

    public static string BuildAchName(string originCode, int sequence)
    {
        if (string.IsNullOrWhiteSpace(originCode) || !originCode.All(char.IsDigit) || originCode.Length < 7)
        {
            throw new InvalidOperationException("Para ACH el origin code debe contener exactamente 7 dígitos (RRRRTTT).");
        }

        var normalizedOriginCode = originCode[^7..];
        return $"{normalizedOriginCode}.{sequence:D3}.1";
    }

    public static string ReplaceRecord1FileIdModifier(string nachaContent, char expectedIdentifier)
    {
        if (string.IsNullOrEmpty(nachaContent))
        {
            return nachaContent;
        }

        string[] lines = nachaContent.Split('\n');
        if (lines.Length == 0)
        {
            return nachaContent;
        }

        string firstLine = lines[0].TrimEnd('\r');
        if (firstLine.Length < 36)
        {
            return nachaContent;
        }

        char[] chars = firstLine.ToCharArray();
        chars[35] = expectedIdentifier;
        lines[0] = new string(chars);

        return string.Join('\n', lines);
    }

    public static char? TryExtractRecord1FileIdModifier(string? nachaContent)
    {
        if (string.IsNullOrWhiteSpace(nachaContent))
        {
            return null;
        }

        var firstLine = nachaContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.TrimEnd('\r');
        if (string.IsNullOrEmpty(firstLine) || firstLine.Length < 36)
        {
            return null;
        }

        return firstLine[35];
    }

    public static int CountDetailRecords(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 1 && content.Length % 106 == 0)
        {
            return Enumerable.Range(0, content.Length / 106)
                .Select(idx => content[idx * 106])
                .Count(ch => ch == '6' || ch == '7');
        }

        return lines.Count(line => line.Length > 0 && (line[0] == '6' || line[0] == '7'));
    }
}
