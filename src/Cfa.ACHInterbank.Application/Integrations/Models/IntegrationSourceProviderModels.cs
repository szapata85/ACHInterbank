using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Models;

public enum IntegrationSensitiveClassification
{
    None = 0,
    Personal = 1,
    Account = 2,
    Financial = 3,
    Technical = 4
}

public sealed record IntegrationSourceProviderDescriptor
{
    public IntegrationSourceProviderDescriptor(
        IntegrationSourceKindEnum sourceKind,
        string sourceKey,
        string displayNameEs,
        string descriptionEs,
        string dataType,
        bool nullable,
        string functionalGroup,
        int sortOrder,
        IntegrationSensitiveClassification sensitiveClassification = IntegrationSensitiveClassification.None,
        IEnumerable<string>? supportedMethodCodes = null)
    {
        if (sourceKind == IntegrationSourceKindEnum.Expression)
        {
            throw new NotSupportedException("El motor contract-driven no admite providers de expresiones arbitrarias.");
        }

        SourceKind = sourceKind;
        SourceKey = RequireText(sourceKey, nameof(sourceKey));
        DisplayNameEs = RequireText(displayNameEs, nameof(displayNameEs));
        DescriptionEs = RequireText(descriptionEs, nameof(descriptionEs));
        DataType = RequireText(dataType, nameof(dataType));
        Nullable = nullable;
        FunctionalGroup = RequireText(functionalGroup, nameof(functionalGroup));
        SortOrder = sortOrder;
        SensitiveClassification = sensitiveClassification;
        SupportedMethodCodes = (supportedMethodCodes ?? Array.Empty<string>())
            .Select(item => RequireText(item, nameof(supportedMethodCodes)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IntegrationSourceKindEnum SourceKind { get; }
    public string SourceKey { get; }
    public string DisplayNameEs { get; }
    public string DescriptionEs { get; }
    public string DataType { get; }
    public bool Nullable { get; }
    public string FunctionalGroup { get; }
    public int SortOrder { get; }
    public IntegrationSensitiveClassification SensitiveClassification { get; }
    public IReadOnlyList<string> SupportedMethodCodes { get; }

    public bool SupportsMethod(string methodCode)
    {
        var normalized = RequireText(methodCode, nameof(methodCode));
        return SupportedMethodCodes.Count == 0
            || SupportedMethodCodes.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es obligatorio.", parameterName)
            : value.Trim();
}
