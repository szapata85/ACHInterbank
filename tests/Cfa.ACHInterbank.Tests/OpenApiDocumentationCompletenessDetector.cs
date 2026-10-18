using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Tests;

[Flags]
internal enum OpenApiDocumentationIssue
{
    None = 0,
    SummaryIncomplete = 1,
    DescriptionIncomplete = 2
}

internal static class OpenApiDocumentationCompletenessDetector
{
    private static readonly string[] ExactPlaceholders =
    [
        "Endpoint de la API ACH Interbank.",
        "Consulta información del sistema.",
        "DOCUMENTATION INCOMPLETE"
    ];

    private static readonly Regex GenericModulePlaceholder = new(
        @"^Operación del módulo [^.!?]+\.?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static OpenApiDocumentationIssue Detect(OpenApiOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var issues = OpenApiDocumentationIssue.None;

        if (IsIncompleteText(operation.Summary))
        {
            issues |= OpenApiDocumentationIssue.SummaryIncomplete;
        }

        if (IsIncompleteText(operation.Description))
        {
            issues |= OpenApiDocumentationIssue.DescriptionIncomplete;
        }

        return issues;
    }

    public static bool IsIncomplete(OpenApiOperation operation)
    {
        return Detect(operation) != OpenApiDocumentationIssue.None;
    }

    private static bool IsIncompleteText(string? value)
    {
        var normalized = NormalizeWhitespace(value);
        if (normalized.Length == 0)
        {
            return true;
        }

        if (ExactPlaceholders.Any(placeholder =>
                string.Equals(normalized, placeholder, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return GenericModulePlaceholder.IsMatch(normalized);
    }

    private static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
