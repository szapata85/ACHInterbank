using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class NachaCanonicalMapper : INachaCanonicalMapper
{
    internal enum CanonicalResolutionFailure
    {
        None = 0,
        UnresolvableAlias = 1,
        AmbiguousAlias = 2,
        InvalidCanonicalKey = 3
    }

    internal sealed record CanonicalResolutionProbe(
        bool Success,
        string? CanonicalKey,
        CanonicalResolutionFailure Failure,
        IReadOnlyList<string> Candidates);

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
        },
        ["7"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["addendatype"] = "AddendaType",
            ["tipoaddenda"] = "AddendaType",
            ["typecode"] = "AddendaType",
            ["businesstype"] = "BusinessType",
            ["tiponegocio"] = "BusinessType",
            ["purpose"] = "Purpose",
            ["proposito"] = "Purpose",
            ["descripcionproposito"] = "Purpose",
            ["reference"] = "Reference",
            ["referencia"] = "Reference",
            ["collectorid"] = "CollectorId",
            ["identificacionrecaudador"] = "CollectorId",
            ["receivercustomercode"] = "ReceiverCustomerCode",
            ["codigoclientereceptor"] = "ReceiverCustomerCode",
            ["servicedescription"] = "ServiceDescription",
            ["descripcionservicio"] = "ServiceDescription",
            ["sequencenumber"] = "SequenceNumber",
            ["addendasequence"] = "SequenceNumber",
            ["secuenciaaddenda"] = "SequenceNumber",
            ["tracesuffix"] = "TraceSuffix",
            ["tracenumbersuffix"] = "TraceSuffix",
            ["sufijotrace"] = "TraceSuffix",
            ["returnreasoncode"] = "ReturnReasonCode",
            ["codigodevolucion"] = "ReturnReasonCode",
            ["originaltracenumber"] = "OriginalTraceNumber",
            ["numerotraceoriginal"] = "OriginalTraceNumber",
            ["newtracenumber"] = "NewTraceNumber",
            ["numerotracenuevo"] = "NewTraceNumber",
            ["transactiontracenumber"] = "TransactionTraceNumber",
            ["tracenumber"] = "TransactionTraceNumber",
            ["transactioncode"] = "TransactionCode",
            ["codigotransaccion"] = "TransactionCode",
            ["batchcompanyentrydescription"] = "BatchCompanyEntryDescription",
            ["descripcionentradalote"] = "BatchCompanyEntryDescription"
        }
    };

    public string ResolveCanonicalKey(string recordCode, string keyOrAlias)
    {
        if (TryResolveCanonicalKey(recordCode, keyOrAlias, out var canonicalKey))
        {
            return canonicalKey;
        }

        return keyOrAlias.Trim();
    }

    public bool TryResolveCanonicalKey(string recordCode, string keyOrAlias, out string canonicalKey)
    {
        var probe = Probe(recordCode, keyOrAlias);
        canonicalKey = probe.CanonicalKey ?? string.Empty;
        return probe.Success;
    }

    internal CanonicalResolutionProbe Probe(string recordCode, string keyOrAlias)
    {
        var raw = (keyOrAlias ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new CanonicalResolutionProbe(false, null, CanonicalResolutionFailure.UnresolvableAlias, []);
        }

        var normalized = Normalize(raw);
        var candidates = ResolveCandidates(recordCode, normalized);
        if (candidates.Count == 1)
        {
            return new CanonicalResolutionProbe(true, candidates[0], CanonicalResolutionFailure.None, candidates);
        }

        if (candidates.Count > 1)
        {
            return new CanonicalResolutionProbe(false, null, CanonicalResolutionFailure.AmbiguousAlias, candidates);
        }

        var failure = LooksLikeCanonical(raw)
            ? CanonicalResolutionFailure.InvalidCanonicalKey
            : CanonicalResolutionFailure.UnresolvableAlias;
        return new CanonicalResolutionProbe(false, null, failure, []);
    }

    private static List<string> ResolveCandidates(string recordCode, string normalizedKey)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (RecordOverrides.TryGetValue(recordCode, out var scopedMap))
        {
            if (scopedMap.TryGetValue(normalizedKey, out var scopedAlias))
            {
                candidates.Add(scopedAlias);
            }

            foreach (var scopedCanonical in scopedMap.Values)
            {
                if (Normalize(scopedCanonical) == normalizedKey)
                {
                    candidates.Add(scopedCanonical);
                }
            }
        }

        if (GlobalAliases.TryGetValue(normalizedKey, out var globalAlias))
        {
            candidates.Add(globalAlias);
        }

        foreach (var globalCanonical in GlobalAliases.Values)
        {
            if (Normalize(globalCanonical) == normalizedKey)
            {
                candidates.Add(globalCanonical);
            }
        }

        return candidates.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool LooksLikeCanonical(string keyOrAlias)
    {
        if (string.IsNullOrWhiteSpace(keyOrAlias))
        {
            return false;
        }

        var trimmed = keyOrAlias.Trim();
        if (trimmed.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return false;
        }

        return trimmed.Any(char.IsUpper);
    }

    private static string Normalize(string value)
    {
        return new string((value ?? string.Empty).Trim().Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }
}
