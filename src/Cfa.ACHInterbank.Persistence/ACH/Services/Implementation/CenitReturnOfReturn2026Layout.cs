namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Contrato físico CENIT para Devolución de una Devolución (ROR).
/// Fuente: Manual de Especificaciones Formato NACHA-M CENIT, 07-may-2026,
/// sección 7.1.2 y Anexo 1.7. El contrato específico vigente está definido para PPD.
/// </summary>
internal static class CenitReturnOfReturn2026Layout
{
    public const int RecordLength = 106;
    public const int BlockingFactor = 10;
    public const string InProfileCode = "OFFICIAL_CENIT_ENTRADA_DEVOLUCION_DEVOLUCION_V2026_1_0";
    public const string OutProfileCode = "OFFICIAL_CENIT_SALIDA_DEVOLUCION_DEVOLUCION_V2026_1_0";
    public const string FlowTypeCode = "DEVOLUCION_DEVOLUCION";
    public const string NormativeVersion = "2026-05-07";
    public const string CcdScopeStatus = "CENIT_ROR_CCD_NOT_NORMATIVELY_DEFINED";
    public const string CtxScopeStatus = CenitReturnIn2026Layout.CtxScopeStatus;

    private const string Source = "Manual de Especificaciones Formato NACHA-M CENIT";
    private const string Section = "7.1.2; Anexo 1.7; Tabla 6; CENIT-Anexo-A Tabla 2";
    private static readonly IReadOnlyCollection<string> Causes = Array.AsReadOnly(
        Enumerable.Range(60, 15).Select(value => $"R{value}").ToArray());
    private static readonly HashSet<string> CauseSet = new(Causes, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<AchColOfficialFieldDescriptor> Type7 = Array.AsReadOnly(
    [
        F("T7-RECORDTYPE", "RECORDTYPE", 1, 1, NachaFieldDataType.Numeric, true, 'L', ' ', ["7"]),
        F("T7-ADDENDA-TYPE", "ADDENDATYPE", 2, 2, NachaFieldDataType.Numeric, true, 'R', '0', ["99"]),
        F("T7-ROR-REASON", "RETURNREASONCODE", 4, 3, NachaFieldDataType.Alphanumeric, true, 'L', ' ', Causes),
        F("T7-ORIGINAL-TRACE", "ORIGINALTRACENUMBER", 7, 15, NachaFieldDataType.Numeric, true, 'R', '0'),
        F("T7-RESERVED-1", "RESERVED1", 22, 8, NachaFieldDataType.Reserved, false, 'L', ' '),
        F("T7-ORIGINAL-RECEIVING-DFI", "ORIGINALRECEIVINGDFI", 30, 8, NachaFieldDataType.Numeric, true, 'R', '0'),
        F("T7-RESERVED-2", "RESERVED2", 38, 3, NachaFieldDataType.Reserved, false, 'L', ' '),
        F("T7-SOURCE-RETURN-TRACE", "SOURCERETURNTRACENUMBER", 41, 15, NachaFieldDataType.Numeric, true, 'R', '0'),
        F("T7-SOURCE-RETURN-DATE", "SOURCERETURNSETTLEMENTDATE", 56, 3, NachaFieldDataType.Numeric, true, 'R', '0'),
        F("T7-SOURCE-RETURN-REASON", "SOURCERETURNREASONCODE", 59, 2, NachaFieldDataType.Numeric, true, 'R', '0'),
        F("T7-RESERVED-3", "RESERVED3", 61, 21, NachaFieldDataType.Reserved, false, 'L', ' '),
        F("T7-ROR-SEQUENCE", "ADDENDASEQUENCENUMBER", 82, 15, NachaFieldDataType.Numeric, true, 'R', '0'),
        F("T7-RESERVED-4", "RESERVED4", 97, 10, NachaFieldDataType.Reserved, false, 'L', ' ')
    ]);

    internal static string Variant(string recordCode, bool inbound)
        => $"CENIT_ROR_{(inbound ? "IN" : "OUT")}_2026_R{recordCode}" + (recordCode == "7" ? "_ADDENDA_99" : string.Empty);

    internal static bool IsProfile(string? profileCode)
        => string.Equals(profileCode, InProfileCode, StringComparison.Ordinal)
           || string.Equals(profileCode, OutProfileCode, StringComparison.Ordinal);

    internal static bool IsCause(string? cause)
        => !string.IsNullOrWhiteSpace(cause) && CauseSet.Contains(cause.Trim());

    internal static IReadOnlyList<AchColOfficialFieldDescriptor> ForRecord(string recordCode)
    {
        if (recordCode == "7") return Type7;

        return CenitReturnIn2026Layout.ForRecord(recordCode)
            .Select(field => field with
            {
                RuleId = field.RuleId.Replace("RETURN-IN", "ROR", StringComparison.Ordinal),
                AllowedValues = recordCode == "5" && field.FieldCode == "STANDARDENTRYCLASSCODE" ? ["PPD"] : field.AllowedValues,
                NormativeSection = Section,
                NormativeSource = Source,
                NormativeVersion = NormativeVersion
            })
            .ToArray();
    }

    internal static AchColOfficialFieldDescriptor Field(string recordCode, string fieldCode)
        => ForRecord(recordCode).Single(field => string.Equals(field.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));

    internal static bool TryParseAddenda(string record, out CenitReturnOfReturnAddenda2026? addenda)
    {
        addenda = null;
        if (record.Length != RecordLength || record[0] != '7' || record.Substring(1, 2) != "99") return false;

        var reason = record.Substring(3, 3).Trim().ToUpperInvariant();
        var originalTrace = record.Substring(6, 15).Trim();
        var originalReceivingDfi = record.Substring(29, 8).Trim();
        var sourceReturnTrace = record.Substring(40, 15).Trim();
        var sourceReturnDate = record.Substring(55, 3).Trim();
        var sourceReturnReason = record.Substring(58, 2).Trim();
        var addendaSequence = record.Substring(81, 15).Trim();
        if (!IsCause(reason)
            || !IsDigits(originalTrace, 15)
            || !IsDigits(originalReceivingDfi, 8)
            || !IsDigits(sourceReturnTrace, 15)
            || !IsDigits(sourceReturnDate, 3)
            || !IsDigits(sourceReturnReason, 2)
            || !IsDigits(addendaSequence, 15)) return false;

        addenda = new(reason, originalTrace, originalReceivingDfi, sourceReturnTrace, sourceReturnDate, sourceReturnReason, addendaSequence);
        return true;
    }

    private static bool IsDigits(string value, int length) => value.Length == length && value.All(char.IsDigit);

    private static AchColOfficialFieldDescriptor F(
        string suffix, string fieldCode, int start, int length, NachaFieldDataType type,
        bool required, char justification, char pad, IReadOnlyCollection<string>? allowed = null)
        => new($"CENIT-2026-ROR-{suffix}", "7", fieldCode, start, length, type, required,
            justification, pad, AllowedValues: allowed, NormativeSection: Section,
            NormativeSource: Source, NormativeVersion: NormativeVersion);
}

internal sealed record CenitReturnOfReturnAddenda2026(
    string ReasonCode,
    string OriginalTraceNumber,
    string OriginalReceivingDfi,
    string SourceReturnTraceNumber,
    string SourceReturnSettlementDate,
    string SourceReturnReasonCode,
    string AddendaSequenceNumber);
