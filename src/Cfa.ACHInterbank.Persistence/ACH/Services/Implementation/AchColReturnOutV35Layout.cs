using System.Collections.ObjectModel;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>Descriptor ejecutable de ReturnOut ACH Colombia según V35, sección 6.6.</summary>
internal static class AchColReturnOutV35Layout
{
    public const int RecordLength = 106;
    public const int BlockingFactor = 10;
    public const string VariantPrefix = "ACH_RETURN_OUT_V35_R";
    public const string Addenda99Variant = "ACH_RETURN_OUT_V35_R7_ADDENDA_99";

    private const string Source = "ACH Colombia Manual de Servicio V35";
    private const string Version = "V35";
    private const string Section = "6.6 Ficha técnica transacción devolución";

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>> Layouts =
        new ReadOnlyDictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>>(
            new Dictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>>(StringComparer.OrdinalIgnoreCase)
            {
                ["1"] = Fields(
                    F("T1-RECORDTYPE", "1", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["1"]),
                    F("T1-PRIORITY", "1", "PRIORITYCODE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["01"]),
                    F("T1-DESTINATION", "1", "IMMEDIATEDESTINATION", 4, 10, NachaFieldDataType.Numeric, true, 'R', ' '),
                    F("T1-ORIGIN", "1", "IMMEDIATEORIGIN", 14, 10, NachaFieldDataType.Numeric, true, 'R', ' '),
                    F("T1-CREATION-DATE", "1", "FILECREATIONDATE", 24, 8, NachaFieldDataType.Date, true, 'R', '0', "yyyyMMdd"),
                    F("T1-CREATION-TIME", "1", "FILECREATIONTIME", 32, 4, NachaFieldDataType.Time, false, 'R', '0', "HHmm"),
                    F("T1-FILE-ID", "1", "FILEIDMODIFIER", 36, 1, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("T1-RECORD-SIZE", "1", "RECORDSIZE", 37, 3, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["106"]),
                    F("T1-BLOCKING", "1", "BLOCKINGFACTOR", 40, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["10"]),
                    F("T1-FORMAT", "1", "FORMATCODE", 42, 1, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["1"]),
                    F("T1-DESTINATION-NAME", "1", "IMMEDIATEDESTINATIONNAME", 43, 23, NachaFieldDataType.Alphanumeric, false, 'L', ' '),
                    F("T1-ORIGIN-NAME", "1", "IMMEDIATEORIGINNAME", 66, 23, NachaFieldDataType.Alphanumeric, false, 'L', ' '),
                    F("T1-REFERENCE", "1", "REFERENCECODE", 89, 8, NachaFieldDataType.Alphanumeric, false, 'L', ' '),
                    F("T1-RESERVED", "1", "RESERVED", 97, 10, NachaFieldDataType.Reserved, false, 'L', ' ')),
                ["5"] = Fields(
                    F("T5-RECORDTYPE", "5", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["5"]),
                    F("T5-SERVICE-CLASS", "5", "SERVICECLASSCODE", 2, 3, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T5-COMPANY-NAME", "5", "COMPANYNAME", 5, 16, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("T5-DISCRETIONARY", "5", "COMPANYDISCRETIONARYDATA", 21, 20, NachaFieldDataType.Alphanumeric, false, 'L', ' '),
                    F("T5-COMPANY-ID", "5", "COMPANYIDENTIFICATION", 41, 10, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("T5-SEC", "5", "STANDARDENTRYCLASSCODE", 51, 3, NachaFieldDataType.Alphanumeric, true, 'L', ' ', allowed: ["PPD"]),
                    F("T5-DESCRIPTION", "5", "COMPANYENTRYDESCRIPTION", 54, 10, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("T5-DESCRIPTIVE-DATE", "5", "COMPANYDESCRIPTIVEDATE", 64, 8, NachaFieldDataType.Date, false, 'R', '0', "yyyyMMdd"),
                    F("T5-EFFECTIVE-DATE", "5", "EFFECTIVEENTRYDATE", 72, 8, NachaFieldDataType.Date, true, 'R', '0', "yyyyMMdd"),
                    F("T5-SETTLEMENT-DATE", "5", "SETTLEMENTDATE", 80, 3, NachaFieldDataType.Numeric, false, 'R', ' '),
                    F("T5-ORIGINATOR-STATUS", "5", "ORIGINATORSTATUSCODE", 83, 1, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["1"]),
                    F("T5-ORIGINATING-DFI", "5", "ORIGINATINGDFI", 84, 8, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T5-BATCH-NUMBER", "5", "BATCHNUMBER", 92, 7, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T5-RESERVED", "5", "RESERVED", 99, 8, NachaFieldDataType.Reserved, false, 'L', ' ')),
                ["6"] = Fields(
                    F("T6-RECORDTYPE", "6", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["6"]),
                    F("T6-TRANSACTION-CODE", "6", "TRANSACTIONCODE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["21", "31", "51", "26", "36", "56"]),
                    F("T6-RECEIVING-DFI", "6", "RECEIVINGDFI", 4, 8, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T6-CHECK-DIGIT", "6", "CHECKDIGIT", 12, 1, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T6-ACCOUNT", "6", "DFIACCOUNTNUMBER", 13, 17, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("T6-AMOUNT", "6", "AMOUNT", 30, 18, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T6-INDIVIDUAL-ID", "6", "INDIVIDUALIDENTIFICATION", 48, 15, NachaFieldDataType.Alphanumeric, false, 'L', ' '),
                    F("T6-INDIVIDUAL-NAME", "6", "INDIVIDUALNAME", 63, 22, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("T6-DISCRETIONARY", "6", "DISCRETIONARYDATA", 85, 2, NachaFieldDataType.Alphanumeric, false, 'L', ' '),
                    F("T6-ADDENDA", "6", "ADDENDARECORDINDICATOR", 87, 1, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["1"]),
                    F("T6-TRACE", "6", "TRACENUMBER", 88, 15, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T6-RESERVED", "6", "RESERVED", 103, 4, NachaFieldDataType.Reserved, false, 'L', ' ')),
                ["7"] = Fields(
                    F("T7-RECORDTYPE", "7", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["7"]),
                    F("T7-ADDENDA-TYPE", "7", "ADDENDATYPE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["99"]),
                    F("T7-RETURN-REASON", "7", "RETURNREASONCODE", 4, 3, NachaFieldDataType.Alphanumeric, true, 'L', ' ',
                        allowed: ["R01", "R02", "R03", "R04", "R06", "R07", "R08", "R09", "R10", "R12", "R13", "R14", "R15", "R16", "R17", "R20", "R23", "R29", "R30", "R31", "R32", "R33", "R35"]),
                    F("T7-ORIGINAL-TRACE", "7", "ORIGINALTRACENUMBER", 7, 15, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T7-DEATH-DATE", "7", "DATEOFDEATH", 22, 8, NachaFieldDataType.Date, false, 'L', ' ', "yyyyMMdd"),
                    F("T7-ORIGINAL-RECEIVING-DFI", "7", "ORIGINALRECEIVINGDFI", 30, 8, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T7-ADDITIONAL-INFORMATION", "7", "ADDITIONALINFORMATION", 38, 44, NachaFieldDataType.Alphanumeric, false, 'L', ' '),
                    F("T7-ADDENDA-SEQUENCE", "7", "ADDENDASEQUENCENUMBER", 82, 15, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T7-RESERVED", "7", "RESERVED", 97, 10, NachaFieldDataType.Reserved, false, 'L', ' ')),
                ["8"] = Fields(
                    F("T8-RECORDTYPE", "8", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["8"]),
                    F("T8-SERVICE-CLASS", "8", "SERVICECLASSCODE", 2, 3, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T8-COUNT", "8", "ENTRYADDENDACOUNT", 5, 6, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T8-HASH", "8", "ENTRYHASH", 11, 10, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T8-DEBITS", "8", "TOTALDEBITAMOUNT", 21, 18, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T8-CREDITS", "8", "TOTALCREDITAMOUNT", 39, 18, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T8-COMPANY-ID", "8", "COMPANYIDENTIFICATION", 57, 10, NachaFieldDataType.Alphanumeric, true, 'L', ' '),
                    F("T8-MAC", "8", "MESSAGEAUTHENTICATIONCODE", 67, 19, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("T8-RESERVED", "8", "RESERVED", 86, 6, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("T8-ORIGINATING-DFI", "8", "ORIGINATINGDFI", 92, 8, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T8-BATCH-NUMBER", "8", "BATCHNUMBER", 100, 7, NachaFieldDataType.Numeric, true, 'R', '0')),
                ["9"] = Fields(
                    F("T9-RECORDTYPE", "9", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["9"]),
                    F("T9-BATCH-COUNT", "9", "BATCHCOUNT", 2, 6, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T9-BLOCK-COUNT", "9", "BLOCKCOUNT", 8, 6, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T9-COUNT", "9", "ENTRYADDENDACOUNT", 14, 8, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T9-HASH", "9", "ENTRYHASH", 22, 10, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T9-DEBITS", "9", "TOTALDEBITAMOUNT", 32, 18, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T9-CREDITS", "9", "TOTALCREDITAMOUNT", 50, 18, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T9-RESERVED", "9", "RESERVED", 68, 39, NachaFieldDataType.Reserved, false, 'L', ' '))
            });

    internal static string Variant(string recordCode) => recordCode == "7" ? Addenda99Variant : $"{VariantPrefix}{recordCode}";
    internal static bool IsVariant(string? variantCode) => variantCode?.StartsWith(VariantPrefix, StringComparison.OrdinalIgnoreCase) == true;
    internal static IReadOnlyList<AchColOfficialFieldDescriptor> ForRecord(string recordCode) => Layouts[recordCode];
    internal static AchColOfficialFieldDescriptor Field(string recordCode, string fieldCode) =>
        ForRecord(recordCode).Single(field => string.Equals(field.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<AchColOfficialFieldDescriptor> Fields(params AchColOfficialFieldDescriptor[] fields) => Array.AsReadOnly(fields);

    private static AchColOfficialFieldDescriptor F(
        string suffix, string recordCode, string fieldCode, int start, int length,
        NachaFieldDataType dataType, bool required, char justification, char pad,
        string? format = null, IReadOnlyCollection<string>? allowed = null) =>
        new($"ACHCOL-V35-RETURN-{suffix}", recordCode, fieldCode, start, length, dataType, required,
            justification, pad, format, allowed, NormativeSection: Section, NormativeSource: Source, NormativeVersion: Version);
}
