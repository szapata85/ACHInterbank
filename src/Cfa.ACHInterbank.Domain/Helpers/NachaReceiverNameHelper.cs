using System.Globalization;
using System.Text;

namespace Cfa.ACHInterbank.Domain.Helpers;

public static class NachaReceiverNameHelper
{
    public const int FieldLength = 22;

    private static readonly HashSet<char> AllowedSpecialCharacters =
    [
        '.', ',', ';', ':', 'Ñ', '-', '*', '/', '&', '#', '$', '%', '='
    ];

    public static string SanitizeForType6(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        var normalized = RemoveDiacritics(rawName).ToUpperInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character) || character == ' ' || AllowedSpecialCharacters.Contains(character))
            {
                builder.Append(character);
            }
        }

        var compact = string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (compact.Length > FieldLength)
        {
            compact = compact[..FieldLength];
        }

        return compact;
    }

    public static string? ValidateType6RawField(string? rawField)
    {
        var value = rawField ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Error Fatal ID 22: el Nombre del Usuario Receptor (posiciones 63-84) es obligatorio y no puede estar vacío.";
        }

        if (value.Length > 0 && value[0] == ' ')
        {
            return "Error Fatal ID 26: el Nombre del Usuario Receptor (posiciones 63-84) debe estar justificado a la izquierda.";
        }

        foreach (var character in value.TrimEnd())
        {
            if (char.IsLetterOrDigit(character) || character == ' ' || AllowedSpecialCharacters.Contains(character))
            {
                continue;
            }

            return "Error Fatal ID 27: el Nombre del Usuario Receptor contiene caracteres no permitidos por el estándar ACH.";
        }

        return null;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
