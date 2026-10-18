namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record IncomingNachaAdmissionRequest(
    string FileName,
    IReadOnlyList<string> Records,
    IncomingNachaCycleResolutionResult Resolution,
    bool IsExplicitReprocess);

/// <summary>Incidencia registrada durante la admisión de una ingesta NACHA-M.</summary>
/// <param name="Code">Código de la incidencia.</param>
/// <param name="Title">Título funcional de la incidencia.</param>
/// <param name="Message">Mensaje registrado para la incidencia.</param>
/// <param name="SuggestedAction">Acción sugerida registrada.</param>
/// <param name="ErrorType">Clasificación textual del error.</param>
/// <param name="Severity">Severidad textual registrada.</param>
/// <param name="ExpectedValue">Valor esperado cuando aplica.</param>
/// <param name="FoundValue">Valor encontrado cuando aplica.</param>
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
