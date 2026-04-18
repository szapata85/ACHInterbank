using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaType7FieldValueResolver : INachaType7FieldValueResolver
{
    private readonly INachaType7AliasMap _aliasMap;

    public NachaType7FieldValueResolver(INachaType7AliasMap aliasMap)
    {
        _aliasMap = aliasMap;
    }

    public IReadOnlyDictionary<string, object?> Resolve(AchBatch batch, AchTransaction transaction, AchTransactionAddenda addenda)
    {
        var traceSuffix = GetTraceSuffix(transaction.TraceNumber);

        var canonicalValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RecordCode"] = "7",
            ["AddendaType"] = addenda.AddendaType,
            ["BusinessType"] = addenda.BusinessType.ToString().ToUpperInvariant(),
            ["Purpose"] = addenda.Purpose ?? batch.CompanyEntryDescription,
            ["Reference"] = addenda.Reference,
            ["CollectorId"] = addenda.CollectorId,
            ["ReceiverCustomerCode"] = addenda.ReceiverCustomerCode,
            ["ServiceDescription"] = addenda.ServiceDescription,
            ["SequenceNumber"] = addenda.SequenceNumber ?? 1,
            ["TraceSuffix"] = traceSuffix,
            ["ReturnReasonCode"] = addenda.ReturnReasonCode,
            ["OriginalTraceNumber"] = addenda.OriginalTraceNumber,
            ["NewTraceNumber"] = addenda.NewTraceNumber,
            ["TransactionTraceNumber"] = transaction.TraceNumber,
            ["TransactionCode"] = transaction.TransactionCode,
            ["BatchCompanyEntryDescription"] = batch.CompanyEntryDescription
        };

        var expanded = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var aliasTrace = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in canonicalValues)
        {
            expanded[pair.Key] = pair.Value;
            var aliases = _aliasMap.GetAliases(pair.Key).ToArray();
            aliasTrace[pair.Key] = aliases;
            foreach (var alias in aliases)
            {
                expanded[alias] = pair.Value;
            }
        }

        expanded["__Type7AliasTrace"] = aliasTrace;
        return expanded;
    }

    private static string GetTraceSuffix(string? traceNumber)
    {
        var digits = new string((traceNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length <= 7 ? digits : digits[^7..];
    }
}
