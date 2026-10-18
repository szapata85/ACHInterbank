namespace Cfa.ACHInterbank.Application.Integrations.Models;

public sealed record IntegrationTransformationDescriptor
{
    public IntegrationTransformationDescriptor(
        string code,
        IEnumerable<string> inputTypes,
        string outputType,
        bool requiresMask,
        bool supportsNull,
        string displayNameEs,
        string descriptionEs)
    {
        Code = RequireText(code, nameof(code));
        var inputs = (inputTypes ?? throw new ArgumentNullException(nameof(inputTypes)))
            .Select(item => RequireText(item, nameof(inputTypes)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (inputs.Length == 0)
        {
            throw new ArgumentException("Debe declararse al menos un InputType.", nameof(inputTypes));
        }

        InputTypes = inputs;
        OutputType = RequireText(outputType, nameof(outputType));
        RequiresMask = requiresMask;
        SupportsNull = supportsNull;
        DisplayNameEs = RequireText(displayNameEs, nameof(displayNameEs));
        DescriptionEs = RequireText(descriptionEs, nameof(descriptionEs));
    }

    public string Code { get; }
    public IReadOnlyList<string> InputTypes { get; }
    public string OutputType { get; }
    public bool RequiresMask { get; }
    public bool SupportsNull { get; }
    public string DisplayNameEs { get; }
    public string DescriptionEs { get; }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es obligatorio.", parameterName)
            : value.Trim();
}

public sealed record IntegrationTransformationContext
{
    public IntegrationTransformationContext(
        string methodCode,
        string parameterPath,
        string targetDataType,
        string? formatMask = null)
    {
        MethodCode = RequireText(methodCode, nameof(methodCode));
        ParameterPath = RequireText(parameterPath, nameof(parameterPath));
        TargetDataType = RequireText(targetDataType, nameof(targetDataType));
        FormatMask = string.IsNullOrWhiteSpace(formatMask) ? null : formatMask.Trim();
    }

    public string MethodCode { get; }
    public string ParameterPath { get; }
    public string TargetDataType { get; }
    public string? FormatMask { get; }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es obligatorio.", parameterName)
            : value.Trim();
}
