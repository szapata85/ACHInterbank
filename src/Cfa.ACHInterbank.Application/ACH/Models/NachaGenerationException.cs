namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaGenerationException : InvalidOperationException
{
    public NachaGenerationException(string code, string message)
        : base($"{code}: {message}")
    {
        Code = code;
    }

    public NachaGenerationException(
        string code,
        string message,
        string? ruleId,
        string? chamber,
        string? recordType,
        string? fieldName,
        string? cause,
        int? startPosition = null,
        int? expectedLength = null)
        : base(BuildSafeMessage(code, message, ruleId, chamber, recordType, fieldName, cause, startPosition, expectedLength))
    {
        Code = code;
        RuleId = ruleId;
        Chamber = chamber;
        RecordType = recordType;
        FieldName = fieldName;
        Cause = cause;
        StartPosition = startPosition;
        ExpectedLength = expectedLength;
    }

    public string Code { get; }
    public string? RuleId { get; }
    public string? Chamber { get; }
    public string? RecordType { get; }
    public string? FieldName { get; }
    public string? Cause { get; }
    public int? StartPosition { get; }
    public int? ExpectedLength { get; }

    private static string BuildSafeMessage(
        string code,
        string message,
        string? ruleId,
        string? chamber,
        string? recordType,
        string? fieldName,
        string? cause,
        int? startPosition,
        int? expectedLength)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(ruleId)) details.Add($"RuleId={ruleId}");
        if (!string.IsNullOrWhiteSpace(chamber)) details.Add($"Cámara={chamber}");
        if (!string.IsNullOrWhiteSpace(recordType)) details.Add($"Registro={recordType}");
        if (!string.IsNullOrWhiteSpace(fieldName)) details.Add($"Campo={fieldName}");
        if (startPosition.HasValue) details.Add($"Posición={startPosition.Value}");
        if (expectedLength.HasValue) details.Add($"LongitudEsperada={expectedLength.Value}");
        if (!string.IsNullOrWhiteSpace(cause)) details.Add($"Causa={cause}");

        return details.Count == 0
            ? $"{code}: {message}"
            : $"{code}: {message} {string.Join("; ", details)}";
    }
}
