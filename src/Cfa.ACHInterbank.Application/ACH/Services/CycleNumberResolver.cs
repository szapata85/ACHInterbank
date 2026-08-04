using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public sealed partial class CycleNumberResolver : ICycleNumberResolver
{
    public int? Resolve(string? cycleName)
    {
        if (string.IsNullOrWhiteSpace(cycleName))
        {
            return null;
        }

        var match = CycleNumberPattern().Match(cycleName.Trim());
        return match.Success && int.TryParse(match.Groups[1].Value, out var cycleNumber)
            ? cycleNumber
            : null;
    }

    [GeneratedRegex(@"(?:^|\b)(?:ciclo|cycle)\s*[-#:]*\s*(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CycleNumberPattern();
}

