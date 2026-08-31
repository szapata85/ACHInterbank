namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Identidad de los perfiles ordinarios de entrada CENIT. La sección 7.3.1 conserva
/// los formatos T6/T7 originados y los Anexos 1.1, 1.2, 1.8 y 1.9 mantienen los
/// formatos físicos comunes; por eso estos perfiles reutilizan los descriptores
/// oficiales publicados para salida y sólo agregan metadatos de dirección.
/// </summary>
internal static class CenitOrdinaryInbound2026Layout
{
    public const string OriginalProfileCode = "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_0";
    public const string PrenotificationProfileCode = "OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_0";
    public const string CtxOriginalProfileCode = "OFFICIAL_CENIT_CTX_ENTRADA_ORIGINAL_V1_0";
    public const string CtxPrenotificationProfileCode = "OFFICIAL_CENIT_CTX_ENTRADA_PRENOTIFICACION_V1_0";
    public const string NormativeVersion = CenitOrdinaryOutbound2026Layout.NormativeVersion;
    public const string VariantPrefix = "CENIT_ORDINARY_IN_2026_R";
    public const string CtxVariantPrefix = "CENIT_CTX_IN_2026_R";

    internal static bool IsProfile(string? profileCode)
        => IsPpdCcdProfile(profileCode) || IsCtxProfile(profileCode);

    internal static bool IsPpdCcdProfile(string? profileCode)
        => string.Equals(profileCode, OriginalProfileCode, StringComparison.Ordinal)
           || string.Equals(profileCode, PrenotificationProfileCode, StringComparison.Ordinal);

    internal static bool IsCtxProfile(string? profileCode)
        => string.Equals(profileCode, CtxOriginalProfileCode, StringComparison.Ordinal)
           || string.Equals(profileCode, CtxPrenotificationProfileCode, StringComparison.Ordinal);

    internal static bool IsPrenotificationProfile(string? profileCode)
        => string.Equals(profileCode, PrenotificationProfileCode, StringComparison.Ordinal)
           || string.Equals(profileCode, CtxPrenotificationProfileCode, StringComparison.Ordinal);

    internal static string Variant(string profileCode, string recordCode)
        => $"{(IsCtxProfile(profileCode) ? CtxVariantPrefix : VariantPrefix)}{recordCode}";

    internal static IReadOnlyList<AchColOfficialFieldDescriptor> ForRecord(string profileCode, string recordCode)
        => IsCtxProfile(profileCode)
            ? CenitCtxOutbound2026Layout.ForRecord(recordCode)
            : CenitOrdinaryOutbound2026Layout.ForRecord(recordCode);

    internal static AchColOfficialFieldDescriptor Field(string profileCode, string recordCode, string fieldCode)
        => ForRecord(profileCode, recordCode)
            .Single(field => string.Equals(field.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));
}
