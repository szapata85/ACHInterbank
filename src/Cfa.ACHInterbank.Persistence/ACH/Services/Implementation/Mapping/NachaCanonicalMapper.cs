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
