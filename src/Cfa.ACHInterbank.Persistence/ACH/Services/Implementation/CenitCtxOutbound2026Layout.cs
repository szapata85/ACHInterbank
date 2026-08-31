using System.Collections.ObjectModel;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Contrato físico ejecutable para operaciones CTX salientes CENIT.
/// Fuente: Manual de Especificaciones Formato NACHA-M CENIT, 07-may-2026,
/// secciones 3.2, 5.1, 5.2 y 6.2; Anexo 1.2, 1.4, 1.5, 1.8 y 1.9;
/// Anexo 2, tablas 4, 5, 6, 8, 9 y 10.
/// </summary>
internal static class CenitCtxOutbound2026Layout
{
    public const int RecordLength = CenitOrdinaryOutbound2026Layout.RecordLength;
    public const int MaxAddendaPerEntry = 9_999;
    public const string OriginalProfileCode = "OFFICIAL_CENIT_CTX_SALIDA_ORIGINAL_V1_0";
    public const string PrenotificationProfileCode = "OFFICIAL_CENIT_CTX_SALIDA_PRENOTIFICACION_V1_0";
    public const string NormativeVersion = CenitOrdinaryOutbound2026Layout.NormativeVersion;
    public const string VariantPrefix = "CENIT_CTX_OUT_2026_R";

    private const string Source = "Manual de Especificaciones Formato NACHA-M CENIT";
    private const string Section = "3.2;5.1;5.2;6.2;Anexo 1.2;Anexo 1.4-1.5;Anexo 1.8-1.9;Anexo 2 Tablas 4-6,8-10";

    private static readonly IReadOnlyCollection<string> TransactionCodes = Array.AsReadOnly(
    [
        "22", "32", "42", "52",
        "23", "33", "43", "53",
        "27", "37", "47", "55",
        "28", "38", "48", "57"
    ]);

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>> Layouts =
        new ReadOnlyDictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>>(
            new Dictionary<string, IReadOnlyList<AchColOfficialFieldDescriptor>>(StringComparer.OrdinalIgnoreCase)
            {
                ["1"] = CenitOrdinaryOutbound2026Layout.ForRecord("1"),
                ["5"] = Array.AsReadOnly(CenitOrdinaryOutbound2026Layout.ForRecord("5")
                    .Select(field => field.FieldCode == "STANDARDENTRYCLASSCODE"
                        ? field with
                        {
                            RuleId = "CENIT-2026-CTX-OUT-T5-SEC",
                            AllowedValues = Array.AsReadOnly(new[] { "CTX" }),
                            NormativeSection = Section,
                            NormativeSource = Source
                        }
                        : field)
                    .ToArray()),
                ["6"] = Fields(
                    F("T6-RECORD-TYPE", "6", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["6"]),
                    F("T6-TRANSACTION-CODE", "6", "TRANSACTIONCODE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: TransactionCodes),
                    F("T6-RECEIVING-DFI", "6", "RECEIVINGDFI", 4, 8, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("T6-CHECK-DIGIT", "6", "CHECKDIGIT", 12, 1, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T6-ACCOUNT", "6", "DFIACCOUNTNUMBER", 13, 17, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Financial),
                    F("T6-AMOUNT", "6", "AMOUNT", 30, 18, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("T6-RECEIVER-ID", "6", "INDIVIDUALIDENTIFICATION", 48, 15, NachaFieldDataType.Alphanumeric, false, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("T6-ADDENDA-COUNT", "6", "ADDENDACOUNT", 63, 4, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T6-RECEIVER-NAME", "6", "INDIVIDUALNAME", 67, 16, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Personal),
                    F("T6-RESERVED-1", "6", "RESERVED1", 83, 2, NachaFieldDataType.Reserved, false, 'L', ' '),
                    F("T6-DISCRETIONARY", "6", "DISCRETIONARYDATA", 85, 2, NachaFieldDataType.Alphanumeric, false, 'L', ' ', sensitivity: NachaFieldSensitivity.Correlatable),
                    F("T6-ADDENDA-INDICATOR", "6", "ADDENDARECORDINDICATOR", 87, 1, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["1"]),
                    F("T6-TRACE", "6", "TRACENUMBER", 88, 15, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("T6-RESERVED-2", "6", "RESERVED2", 103, 4, NachaFieldDataType.Reserved, false, 'L', ' ')),
                ["7"] = Fields(
                    F("T7-RECORD-TYPE", "7", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', allowed: ["7"]),
                    F("T7-ADDENDA-TYPE", "7", "ADDENDATYPE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', allowed: ["05"]),
                    F("T7-PAYMENT-INFORMATION", "7", "PAYMENTRELATEDINFORMATION", 4, 80, NachaFieldDataType.Alphanumeric, true, 'L', ' ', sensitivity: NachaFieldSensitivity.Financial),
                    F("T7-SEQUENCE", "7", "SEQUENCENUMBER", 84, 4, NachaFieldDataType.Numeric, true, 'R', '0'),
                    F("T7-TRACE-SUFFIX", "7", "TRACESUFFIX", 88, 7, NachaFieldDataType.Numeric, true, 'R', '0', sensitivity: NachaFieldSensitivity.Financial),
                    F("T7-RESERVED", "7", "RESERVED", 95, 12, NachaFieldDataType.Reserved, false, 'L', ' ')),
                ["8"] = CenitOrdinaryOutbound2026Layout.ForRecord("8"),
                ["9"] = CenitOrdinaryOutbound2026Layout.ForRecord("9")
            });

    internal static string Variant(string recordCode) => $"{VariantPrefix}{recordCode}";

    internal static bool IsProfile(string? profileCode)
        => string.Equals(profileCode, OriginalProfileCode, StringComparison.Ordinal)
           || string.Equals(profileCode, PrenotificationProfileCode, StringComparison.Ordinal);

    internal static bool IsVariant(string? variantCode)
        => !string.IsNullOrWhiteSpace(variantCode)
           && variantCode.StartsWith(VariantPrefix, StringComparison.OrdinalIgnoreCase);

    internal static IReadOnlyList<AchColOfficialFieldDescriptor> ForRecord(string recordCode) => Layouts[recordCode];

    internal static AchColOfficialFieldDescriptor Field(string recordCode, string fieldCode)
        => ForRecord(recordCode).Single(field => string.Equals(field.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<AchColOfficialFieldDescriptor> Fields(params AchColOfficialFieldDescriptor[] fields)
        => Array.AsReadOnly(fields);

    private static AchColOfficialFieldDescriptor F(
        string suffix,
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
        => new($"CENIT-2026-CTX-OUT-{suffix}", recordCode, fieldCode, start, length, dataType, required,
            justification, pad, format, allowed, sensitivity, NormativeSection: Section, NormativeSource: Source, NormativeVersion: NormativeVersion);
}
