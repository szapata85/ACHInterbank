using System.Collections.ObjectModel;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal enum NachaFieldDataType
{
    Alphanumeric,
    Numeric,
    Date,
    Time,
    Reserved
}

internal enum NachaFieldSensitivity
{
    None,
    Personal,
    Financial,
    Correlatable
}

internal sealed record AchColOfficialFieldDescriptor(
    string RuleId,
    string RecordCode,
    string FieldCode,
    int StartPosition,
    int Length,
    NachaFieldDataType DataType,
    bool Required,
    char Justification,
    char PadChar,
    string? Format = null,
    IReadOnlyCollection<string>? AllowedValues = null,
    NachaFieldSensitivity Sensitivity = NachaFieldSensitivity.None,
    string Severity = "ERROR",
    string NormativeSection = "Secciones 6.4 y 6.5",
    string NormativeSource = "DDS-DIS-MAN-004, Manual de Servicio ACH Colombia",
    string NormativeVersion = "V35")
{
    public int EndPosition => StartPosition + Length - 1;
    public string OverflowPolicy => "REJECT";
    public string Normalizer => "NONE";
}

/// <summary>
/// Snapshot inmutable del layout ordinario ACH Colombia demostrado por MAN-004 V35.
/// No representa ni se reutiliza como especificación CENIT.
/// </summary>
internal static class AchColOfficialNachaLayout
{
    public const int RecordLength = 106;
    public const int BlockingFactor = 10;

    public const string OutboundOriginalProfileCode = "OFFICIAL_ACH_SALIDA_ORIGINAL_V35_0";
    public const string OutboundPrenotificationProfileCode = "OFFICIAL_ACH_SALIDA_PRENOTIFICACION_V35_0";
    public const string InboundOriginalProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V35_0";
    public const string InboundPrenotificationProfileCode = "OFFICIAL_ACH_ENTRADA_PRENOTIFICACION_V35_0";
    public const string NormativeVersion = "V35";
    public const int ProfileVersionMajor = 35;
    public const int ProfileVersionMinor = 0;

    public const string Type7CreditMonetaryVariant = "ACH_R7_CREDIT_MONETARY_V35";
    public const string Type7CreditPrenotificationVariant = "ACH_R7_CREDIT_PRENOTE_V35";
    public const string Type7DebitVariant = "ACH_R7_DEBIT_V35";

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>> Layouts =
        new ReadOnlyDictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>>(
            new Dictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>>(StringComparer.OrdinalIgnoreCase)
            {
                ["1"] = Fields(
                    F("ACHCOL-PHYSICAL-RECORD-LENGTH", "1", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["1"]),
                    F("ACHCOL-T1-PRIORITY-CODE", "1", "PRIORITYCODE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["01"]),
                    F("ACHCOL-T1-IMMEDIATE-DESTINATION", "1", "IMMEDIATEDESTINATION", 4, 10, NachaFieldDataType.Numeric, true, 'R', ' ', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T1-IMMEDIATE-ORIGIN", "1", "IMMEDIATEORIGIN", 14, 10, NachaFieldDataType.Numeric, true, 'R', ' ', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T1-FILE-CREATION-DATE", "1", "FILECREATIONDATE", 24, 8, NachaFieldDataType.Date, true, 'R', '0', "yyyyMMdd"),
                    F("ACHCOL-T1-FILE-CREATION-TIME", "1", "FILECREATIONTIME", 32, 4, NachaFieldDataType.Time, false, 'R', '0', "HHmm"),
                    F("ACHCOL-T1-FILE-ID-MODIFIER", "1", "FILEIDMODIFIER", 36, 1, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("ACHCOL-T1-RECORD-SIZE", "1", "RECORDSIZE", 37, 3, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["106"]),
                    F("ACHCOL-T1-BLOCKING-FACTOR", "1", "BLOCKINGFACTOR", 40, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["10"]),
                    F("ACHCOL-T1-FORMAT-CODE", "1", "FORMATCODE", 42, 1, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["1"]),
                    F("ACHCOL-T1-DESTINATION-NAME", "1", "IMMEDIATEDESTINATIONNAME", 43, 23, NachaFieldDataType.Alphanumeric, false, 'L', ' ', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T1-ORIGIN-NAME", "1", "IMMEDIATEORIGINNAME", 66, 23, NachaFieldDataType.Alphanumeric, false, 'L', ' ', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T1-REFERENCE-CODE", "1", "REFERENCECODE", 89, 8, NachaFieldDataType.Alphanumeric, false, 'L', ' ', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T1-RESERVED", "1", "RESERVED", 97, 10, NachaFieldDataType.Reserved, false, 'L', ' ')),

                ["5"] = Fields(
                    F("ACHCOL-PHYSICAL-RECORD-LENGTH", "5", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["5"]),
                    F("ACHCOL-T5-SERVICE-CLASS-CODE", "5", "SERVICECLASSCODE", 2, 3, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("ACHCOL-T5-COMPANY-NAME", "5", "COMPANYNAME", 5, 16, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T5-DISCRETIONARY-DATA", "5", "COMPANYDISCRETIONARYDATA", 21, 20, NachaFieldDataType.Alphanumeric, false, 'L', ' '),
                    F("ACHCOL-T5-COMPANY-IDENTIFICATION", "5", "COMPANYIDENTIFICATION", 41, 10, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T5-SEC-CODE", "5", "STANDARDENTRYCLASSCODE", 51, 3, NachaFieldDataType.Alphanumeric, true, 'L', ' ', allowed: ["PPD", "CCD"]),
                    F("ACHCOL-T5-ENTRY-DESCRIPTION", "5", "COMPANYENTRYDESCRIPTION", 54, 10, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("ACHCOL-T5-DESCRIPTIVE-DATE", "5", "COMPANYDESCRIPTIVEDATE", 64, 8, NachaFieldDataType.Date, false, 'R', '0', "yyyyMMdd"),
                    F("ACHCOL-T5-EFFECTIVE-DATE", "5", "EFFECTIVEENTRYDATE", 72, 8, NachaFieldDataType.Date, true, 'R', '0', "yyyyMMdd"),
                    F("ACHCOL-T5-SETTLEMENT-DATE", "5", "SETTLEMENTDATE", 80, 3, NachaFieldDataType.Numeric, false, 'R', ' '),
                    F("ACHCOL-T5-ORIGINATOR-STATUS", "5", "ORIGINATORSTATUSCODE", 83, 1, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["1"]),
                    F("ACHCOL-T5-ORIGINATING-ENTITY", "5", "ORIGINATINGDFI", 84, 8, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T5-BATCH-NUMBER", "5", "BATCHNUMBER", 92, 7, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T5-RESERVED", "5", "RESERVED", 99, 8, NachaFieldDataType.Reserved, false, 'L', ' ')),

                ["6"] = Fields(
                    F("ACHCOL-PHYSICAL-RECORD-LENGTH", "6", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["6"]),
                    F("ACHCOL-T6-TRANSACTION-CODE", "6", "TRANSACTIONCODE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["22", "23", "27", "28", "32", "33", "37", "38", "52", "53", "55", "57"]),
                    F("ACHCOL-T6-RECEIVING-DFI", "6", "RECEIVINGDFI", 4, 8, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T6-CHECK-DIGIT", "6", "CHECKDIGIT", 12, 1, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("ACHCOL-T6-ACCOUNT-NUMBER", "6", "DFIACCOUNTNUMBER", 13, 17, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T6-AMOUNT", "6", "AMOUNT", 30, 18, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T6-INDIVIDUAL-ID", "6", "INDIVIDUALIDENTIFICATION", 48, 15, NachaFieldDataType.Alphanumeric, false, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T6-INDIVIDUAL-NAME", "6", "INDIVIDUALNAME", 63, 22, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T6-DISCRETIONARY-DATA", "6", "DISCRETIONARYDATA", 85, 2, NachaFieldDataType.Alphanumeric, false, 'L', ' ', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T6-ADDENDA-INDICATOR", "6", "ADDENDARECORDINDICATOR", 87, 1, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["1"]),
                    F("ACHCOL-T6-TRACE-NUMBER", "6", "TRACENUMBER", 88, 15, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T6-RESERVED", "6", "RESERVED", 103, 4, NachaFieldDataType.Reserved, false, 'L', ' ')),

                [Type7CreditMonetaryVariant] = Fields(
                    F("ACHCOL-PHYSICAL-RECORD-LENGTH", "7", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["7"]),
                    F("ACHCOL-T7-ADDENDA-TYPE", "7", "ADDENDATYPE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["05"]),
                    F("ACHCOL-T7-CREDIT-ORIGINATOR-ID", "7", "ORIGINATORIDENTIFICATION", 4, 15, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T7-CREDIT-RESERVED", "7", "RESERVEDPREFIX", 19, 2, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("ACHCOL-T7-CREDIT-PURPOSE", "7", "PURPOSE", 21, 10, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("ACHCOL-T7-CREDIT-INVOICE", "7", "INVOICEORACCOUNTNUMBER", 31, 24, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T7-CREDIT-RESERVED-INVOICE", "7", "RESERVEDINVOICE", 55, 2, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("ACHCOL-T7-CREDIT-FREE-INFORMATION", "7", "ORIGINATORFREEINFORMATION", 57, 24, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T7-CREDIT-RESERVED-FREE", "7", "RESERVEDFREE", 81, 3, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("ACHCOL-T7-SEQUENCE", "7", "SEQUENCENUMBER", 84, 4, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["0001"]),
                    F("ACHCOL-T7-TRACE-SUFFIX-MATCH", "7", "TRACESUFFIX", 88, 7, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T7-RESERVED", "7", "RESERVED", 95, 12, NachaFieldDataType.Reserved, false, 'L', ' ')),

                [Type7CreditPrenotificationVariant] = Fields(
                    F("ACHCOL-PHYSICAL-RECORD-LENGTH", "7", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["7"]),
                    F("ACHCOL-T7-ADDENDA-TYPE", "7", "ADDENDATYPE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["05"]),
                    F("ACHCOL-T7-CREDIT-ORIGINATOR-ID", "7", "ORIGINATORIDENTIFICATION", 4, 15, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T7-CREDIT-RESERVED", "7", "RESERVEDPREFIX", 19, 2, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("ACHCOL-T7-CREDIT-PURPOSE", "7", "PURPOSE", 21, 10, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("ACHCOL-T7-CREDIT-REFERENCE", "7", "REFERENCE", 31, 53, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T7-SEQUENCE", "7", "SEQUENCENUMBER", 84, 4, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["0001"]),
                    F("ACHCOL-T7-TRACE-SUFFIX-MATCH", "7", "TRACESUFFIX", 88, 7, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T7-RESERVED", "7", "RESERVED", 95, 12, NachaFieldDataType.Reserved, false, 'L', ' ')),

                [Type7DebitVariant] = Fields(
                    F("ACHCOL-PHYSICAL-RECORD-LENGTH", "7", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["7"]),
                    F("ACHCOL-T7-ADDENDA-TYPE", "7", "ADDENDATYPE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["05"]),
                    F("ACHCOL-T7-DEBIT-COLLECTOR-ID", "7", "COLLECTORID", 4, 13, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T7-DEBIT-CUSTOMER-CODE", "7", "RECEIVERCUSTOMERCODE", 17, 30, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T7-DEBIT-SERVICE", "7", "SERVICEDESCRIPTION", 47, 15, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("ACHCOL-T7-DEBIT-RESERVED", "7", "RESERVEDDETAIL", 62, 22, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("ACHCOL-T7-SEQUENCE", "7", "SEQUENCENUMBER", 84, 4, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["0001"]),
                    F("ACHCOL-T7-TRACE-SUFFIX-MATCH", "7", "TRACESUFFIX", 88, 7, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T7-RESERVED", "7", "RESERVED", 95, 12, NachaFieldDataType.Reserved, false, 'L', ' ')),

                ["8"] = Fields(
                    F("ACHCOL-PHYSICAL-RECORD-LENGTH", "8", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["8"]),
                    F("ACHCOL-T8-SERVICE-CLASS-CODE", "8", "SERVICECLASSCODE", 2, 3, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("ACHCOL-T8-ENTRY-ADDENDA-COUNT", "8", "ENTRYADDENDACOUNT", 5, 6, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("ACHCOL-T8-ENTRY-HASH", "8", "ENTRYHASH", 11, 10, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T8-DEBIT-TOTAL", "8", "TOTALDEBITAMOUNT", 21, 18, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T8-CREDIT-TOTAL", "8", "TOTALCREDITAMOUNT", 39, 18, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T8-COMPANY-IDENTIFICATION", "8", "COMPANYIDENTIFICATION", 57, 10, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("ACHCOL-T8-MESSAGE-AUTH-CODE", "8", "MESSAGEAUTHENTICATIONCODE", 67, 19, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("ACHCOL-T8-RESERVED", "8", "RESERVED", 86, 6, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("ACHCOL-T8-ORIGINATING-DFI", "8", "ORIGINATINGDFI", 92, 8, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T8-BATCH-NUMBER-MATCH", "8", "BATCHNUMBER", 100, 7, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Correlatable)),

                ["9"] = Fields(
                    F("ACHCOL-PHYSICAL-RECORD-LENGTH", "9", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["9"]),
                    F("ACHCOL-T9-BATCH-COUNT", "9", "BATCHCOUNT", 2, 6, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("ACHCOL-T9-BLOCK-COUNT", "9", "BLOCKCOUNT", 8, 6, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("ACHCOL-T9-ENTRY-ADDENDA-COUNT", "9", "ENTRYADDENDACOUNT", 14, 8, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("ACHCOL-T9-ENTRY-HASH", "9", "ENTRYHASH", 22, 10, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("ACHCOL-T9-DEBIT-TOTAL", "9", "TOTALDEBITAMOUNT", 32, 18, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T9-CREDIT-TOTAL", "9", "TOTALCREDITAMOUNT", 50, 18, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("ACHCOL-T9-RESERVED", "9", "RESERVED", 68, 39, NachaFieldDataType.Reserved, false, 'L', ' '))
            });

    internal static IReadOnlyList<AchColOfficialFieldDescriptor> ForRecord(string recordCode)
        => Layouts.TryGetValue(recordCode, out var fields)
            ? fields
            : throw new InvalidOperationException($"No existe descriptor ACHCOL para RecordCode={recordCode}.");

    internal static IReadOnlyList<AchColOfficialFieldDescriptor> ForVariant(string recordCode, string? variantCode)
    {
        if (string.Equals(recordCode, "7", StringComparison.OrdinalIgnoreCase))
        {
            var key = string.Equals(variantCode, Type7DebitVariant, StringComparison.OrdinalIgnoreCase)
                ? Type7DebitVariant
                : string.Equals(variantCode, Type7CreditMonetaryVariant, StringComparison.OrdinalIgnoreCase)
                    ? Type7CreditMonetaryVariant
                    : Type7CreditPrenotificationVariant;
            return Layouts[key];
        }

        return ForRecord(recordCode);
    }

    internal static AchColOfficialFieldDescriptor Field(string recordCode, string fieldCode, string? variantCode = null)
        => ForVariant(recordCode, variantCode).Single(field =>
            string.Equals(field.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));

    internal static string Read(string record, string recordCode, string fieldCode, string? variantCode = null)
    {
        if (record.Length != RecordLength)
        {
            throw new InvalidOperationException("NACHA_PHYSICAL_RECORD_LENGTH: el registro no tiene 106 caracteres.");
        }

        var field = Field(recordCode, fieldCode, variantCode);
        return record.Substring(field.StartPosition - 1, field.Length);
    }

    private static IReadOnlyList<AchColOfficialFieldDescriptor> Fields(params AchColOfficialFieldDescriptor[] fields)
        => Array.AsReadOnly(fields);

    private static AchColOfficialFieldDescriptor F(
        string ruleId,
        string recordCode,
        string fieldCode,
        int start,
        int length,
        NachaFieldDataType dataType,
        bool required,
        char justification,
        char pad,
        string? format = null,
        IReadOnlyCollection<string>? allowed = null,
        NachaFieldSensitivity sensitivity = NachaFieldSensitivity.None)
        => new(ruleId, recordCode, fieldCode, start, length, dataType, required, justification, pad, format, allowed, sensitivity);
}
