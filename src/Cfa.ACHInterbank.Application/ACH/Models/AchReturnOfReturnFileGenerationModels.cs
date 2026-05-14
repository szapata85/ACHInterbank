namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchReturnOfReturnFileGenerationRequest(
    IReadOnlyCollection<int> ReturnOfReturnFlowIds,
    DateTime GeneratedAtUtc,
    string? RequestedBy = null,
    string? Source = null);

public sealed record AchReturnOfReturnFileGenerationFailure(
    string Code,
    string Message,
    string? Field = null);

public sealed record AchReturnOfReturnFileGenerationResult(
    bool IsGenerated,
    string? FileName,
    string? ContentText,
    byte[]? Content,
    int GeneratedFlowCount,
    IReadOnlyCollection<int> FlowIds,
    IReadOnlyCollection<AchReturnOfReturnFileGenerationFailure> Failures,
    int? AuditId = null,
    string? ContentSha256 = null);
