namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// I06-CENIT: diagnóstico de un artefacto LOCAL. Nunca constituye comprobante
/// de envío, aceptación, liquidación ni conciliación bancaria.
/// </summary>
public sealed record CenitReturnIntegrationDiagnostic(
    Guid PreparationId,
    int OriginalTransactionId,
    string PreparationStatus,
    string ArtifactIntegrity,
    string ReconciliationStatus,
    string CenitConfigurationMode,
    string? ArtifactSha256,
    int ExistingManualReturnCount,
    int ExistingExportTraceCount,
    int UnattributableChamberResponseCount,
    IReadOnlyList<string> Alerts,
    DateTime InspectedAtUtc,
    bool TransmissionEnabled = false,
    bool ChamberAcknowledgementAttributed = false,
    bool FinanciallyReconciled = false);

/// <summary>La lista es acotada, no representa el histórico completo.</summary>
public sealed record CenitReturnIntegrationPage(
    IReadOnlyList<CenitReturnIntegrationDiagnostic> Items,
    bool LimitedWindow,
    int ReturnedItems);
