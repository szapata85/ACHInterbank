using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaType7AliasMap : INachaType7AliasMap
{
    private static readonly IReadOnlyDictionary<string, string[]> CanonicalToAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["AddendaType"] = ["AddendaType", "TipoAddenda", "TypeCode"],
            ["BusinessType"] = ["BusinessType", "TipoNegocio"],
            ["Purpose"] = ["Purpose", "Proposito", "DescripcionProposito"],
            ["Reference"] = ["Reference", "Referencia"],
            ["CollectorId"] = ["CollectorId", "IdentificacionRecaudador"],
            ["ReceiverCustomerCode"] = ["ReceiverCustomerCode", "CodigoClienteReceptor"],
            ["ServiceDescription"] = ["ServiceDescription", "DescripcionServicio"],
            ["SequenceNumber"] = ["SequenceNumber", "AddendaSequence", "SecuenciaAddenda"],
            ["TraceSuffix"] = ["TraceSuffix", "TraceNumberSuffix", "SufijoTrace"],
            ["ReturnReasonCode"] = ["ReturnReasonCode", "CodigoDevolucion"],
            ["OriginalTraceNumber"] = ["OriginalTraceNumber", "NumeroTraceOriginal"],
            ["NewTraceNumber"] = ["NewTraceNumber", "NumeroTraceNuevo"],
            ["TransactionTraceNumber"] = ["TransactionTraceNumber", "TraceNumber"],
            ["TransactionCode"] = ["TransactionCode", "CodigoTransaccion"],
            ["BatchCompanyEntryDescription"] = ["BatchCompanyEntryDescription", "DescripcionEntradaLote"]
        };

    private static readonly Lazy<IReadOnlyDictionary<string, string>> AliasToCanonical = new(() =>
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in CanonicalToAliases)
        {
            foreach (var alias in pair.Value)
            {
                var normalized = NormalizeStatic(alias);
                if (map.TryGetValue(normalized, out var existing) && !string.Equals(existing, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Alias collision detectado para '{alias}' entre '{existing}' y '{pair.Key}'.");
                }

                map[normalized] = pair.Key;
            }
        }

        return map;
    });

    public string Normalize(string value) => NormalizeStatic(value);

    public string GetCanonicalKey(string keyOrAlias)
    {
        var normalized = NormalizeStatic(keyOrAlias);
        return AliasToCanonical.Value.TryGetValue(normalized, out var canonical)
            ? canonical
            : keyOrAlias.Trim();
    }

    public IReadOnlyCollection<string> GetAliases(string canonicalKey)
    {
        if (CanonicalToAliases.TryGetValue(canonicalKey, out var aliases))
        {
            return aliases;
        }

        return [canonicalKey];
    }

    private static string NormalizeStatic(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value.Trim().Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray();
        return chars.Length == 0 ? string.Empty : new string(chars);
    }
}
