namespace Cfa.ACHInterbank.Application.ACH.Models;

public static class NachaTransactionCodeDirectionClassifier
{
    private static readonly HashSet<string> CreditCodes = new(StringComparer.Ordinal)
    {
        "21", "22", "23", "31", "32", "33", "42", "51", "52", "53"
    };

    private static readonly HashSet<string> DebitCodes = new(StringComparer.Ordinal)
    {
        "26", "27", "28", "36", "37", "38", "55", "56", "57"
    };

    private static readonly HashSet<string> PrenotificationCodes = new(StringComparer.Ordinal)
    {
        "23", "33", "53", "28", "38", "57"
    };

    public static bool TryResolve(string? transactionCode, out NachaEntryDirection direction)
    {
        var code = transactionCode?.Trim() ?? string.Empty;
        if (CreditCodes.Contains(code))
        {
            direction = NachaEntryDirection.Credit;
            return true;
        }

        if (DebitCodes.Contains(code))
        {
            direction = NachaEntryDirection.Debit;
            return true;
        }

        direction = default;
        return false;
    }

    public static bool IsPrenotificationCode(string? transactionCode)
        => PrenotificationCodes.Contains(transactionCode?.Trim() ?? string.Empty);
}
