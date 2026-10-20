namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Contrato físico ejecutable para devoluciones salientes CENIT.
/// Fuente: Manual de Especificaciones Formato NACHA-M CENIT, 07-may-2026,
/// sección 7.2.1 y Anexo 1.6. El registro físico de devolución es común
/// con la aplicación descrita en 7.3.1 y se mantiene en un único snapshot.
/// </summary>
internal static class CenitReturnOut2026Layout
{
    public const int RecordLength = CenitReturnIn2026Layout.RecordLength;
    public const int BlockingFactor = 10;
    public const string ProfileCode = "OFFICIAL_CENIT_SALIDA_DEVOLUCION_V2026_1_0";
    public const string NormativeVersion = "2026-05-07";
    public const string VariantPrefix = "CENIT_RETURN_OUT_2026_R";
    public const string Addenda99Variant = "CENIT_RETURN_OUT_2026_R7_ADDENDA_99";

    internal static string Variant(string recordCode)
        => recordCode == "7" ? Addenda99Variant : $"{VariantPrefix}{recordCode}";

    internal static bool IsProfile(string? profileCode)
        => string.Equals(profileCode, ProfileCode, StringComparison.Ordinal);

    internal static bool IsVariant(string? variantCode)
        => !string.IsNullOrWhiteSpace(variantCode)
           && (variantCode.StartsWith(VariantPrefix, StringComparison.OrdinalIgnoreCase)
               || string.Equals(variantCode, Addenda99Variant, StringComparison.OrdinalIgnoreCase));

    internal static IReadOnlyList<AchColOfficialFieldDescriptor> ForRecord(string recordCode)
        => CenitReturnIn2026Layout.ForRecord(recordCode);

    internal static AchColOfficialFieldDescriptor Field(string recordCode, string fieldCode)
        => CenitReturnIn2026Layout.Field(recordCode, fieldCode);
}
