using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class NachaCanonicalMapper : INachaCanonicalMapper
{
    private static readonly Dictionary<string, string> GlobalAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trace"] = "TraceNumber",
        ["tracenumber"] = "TraceNumber",
        ["receiverid"] = "ReceiverId",
        ["receivingdfi"] = "ReceivingDFI",
        ["amount"] = "Amount"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> RecordOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["6"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["transactioncode"] = "TransactionCode",
            ["receivercustomercode"] = "ReceiverCustomerCode",
            ["destinationaccountnumber"] = "DestinationAccountNumber",
            ["receivingdfi"] = "ReceivingDFI",
            ["tracenumber"] = "TraceNumber"
        }
    };

    public string ResolveCanonicalKey(string recordCode, string keyOrAlias)
    {
        var normalized = Normalize(keyOrAlias);
        if (RecordOverrides.TryGetValue(recordCode, out var map) && map.TryGetValue(normalized, out var scoped))
        {
            return scoped;
        }

        if (GlobalAliases.TryGetValue(normalized, out var global))
        {
            return global;
        }

        return keyOrAlias.Trim();
    }

    private static string Normalize(string value)
    {
        return new string((value ?? string.Empty).Trim().Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }
}
