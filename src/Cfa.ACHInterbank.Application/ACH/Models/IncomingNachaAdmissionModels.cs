namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record IncomingNachaAdmissionRequest(
    string FileName,
    IReadOnlyList<string> Records,
    IncomingNachaCycleResolutionResult Resolution,
    bool IsExplicitReprocess);

public sealed record IncomingNachaAdmissionIssue(
    string Code,
    string Title,
    string Message,
    string SuggestedAction,
    string ErrorType,
    string Severity,
    string? ExpectedValue = null,
    string? FoundValue = null);

public sealed record IncomingNachaAdmissionResult(
    bool IsAccepted,
    NachaHeaderPreview? Header,
    DateOnly? FileNameDate,
    DateOnly? EffectiveDate,
    int? CycleNumber,
    IncomingNachaAdmissionIssue? Issue)
{
    public static IncomingNachaAdmissionResult Accepted(
        NachaHeaderPreview header,
        DateOnly? fileNameDate,
        DateOnly? effectiveDate,
        int? cycleNumber)
        => new(true, header, fileNameDate, effectiveDate, cycleNumber, null);

    public static IncomingNachaAdmissionResult Rejected(
        NachaHeaderPreview? header,
        DateOnly? fileNameDate,
        DateOnly? effectiveDate,
        int? cycleNumber,
        IncomingNachaAdmissionIssue issue)
        => new(false, header, fileNameDate, effectiveDate, cycleNumber, issue);
}
