using System.Text.Json.Serialization;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public class GenerateNachaInboundSimulationRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NachaSimulationMode SimulationMode { get; set; } = NachaSimulationMode.IncomingTransactions;
    public string ClearingHouseCode { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NachaInboundSimulationType ScenarioType { get; set; }
    public int? OriginFinancialInstitutionId { get; set; }
    public int? DestinationFinancialInstitutionId { get; set; }
    public string? OriginFinancialInstitutionCode { get; set; }
    public string? DestinationFinancialInstitutionCode { get; set; }
    public int EntriesCount { get; set; } = 1;
    public decimal Amount { get; set; }
    public string ReferencePrefix { get; set; } = string.Empty;
    public DateOnly BusinessDate { get; set; }
    public string CycleCode { get; set; } = string.Empty;
    public IReadOnlyList<string> PendingPrenotificationReferences { get; set; } = [];
    public IReadOnlyList<string> TransactionReferences { get; set; } = [];
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InboundResponseMode? ResponseMode { get; set; }
    public string? ReasonCode { get; set; }
    public string? Notes { get; set; }
}

public sealed record GenerateNachaInboundSimulationResponse(
    Guid SimulationId,
    int Id,
    string FileName,
    string DownloadUrl,
    string EvidenceUrl,
    string Sha256,
    long FileSizeBytes,
    bool GeneratedOnly,
    bool AutoImported,
    bool UploadRequired,
    bool ExternalTransmission,
    string Message);

public sealed record NachaInboundSimulationDto(
    int Id,
    Guid SimulationId,
    string ClearingHouseName,
    NachaInboundSimulationType ScenarioType,
    InboundResponseMode? ResponseMode,
    string? ReasonCode,
    string OriginFinancialInstitution,
    string DestinationFinancialInstitution,
    int OriginFinancialInstitutionId,
    int DestinationFinancialInstitutionId,
    int EntriesCount,
    decimal Amount,
    DateOnly BusinessDate,
    string CycleCode,
    string FileName,
    string Sha256,
    long FileSizeBytes,
    string Status,
    bool GeneratedOnly,
    bool AutoImported,
    bool UploadRequired,
    bool ExternalTransmission,
    DateTimeOffset CreatedAt,
    IReadOnlyList<NachaInboundSimulationEntryDto> Entries);

public sealed record NachaInboundSimulationEntryDto(
    int Id,
    string Reference,
    int? TransactionId,
    string? PrenotificationReference,
    string AccountNumberMasked,
    decimal Amount,
    string Nature,
    string? PreviousStatus,
    string ExpectedStatusAfterUpload,
    string? ReasonCode,
    bool IsSynthetic);

public sealed record NachaInboundSimulationMetadataDto(
    Guid SimulationId,
    string ClearingHouse,
    string ScenarioType,
    string? ResponseMode,
    string? ReasonCode,
    string OriginFinancialInstitution,
    string DestinationFinancialInstitution,
    int OriginFinancialInstitutionId,
    string OriginFinancialInstitutionCode,
    bool OriginIsDefaultSource,
    int DestinationFinancialInstitutionId,
    string DestinationFinancialInstitutionCode,
    bool DestinationIsDefaultSource,
    string DestinationResolvedFrom,
    DateOnly BusinessDate,
    string CycleCode,
    string FileName,
    string Sha256,
    long FileSizeBytes,
    string RecordsDetected,
    int BlockCount,
    int EntryAddendaCount,
    string EntryHash,
    bool GeneratedOnly,
    bool AutoImported,
    bool UploadRequired,
    string UploadFlow,
    bool ExternalTransmission,
    string SimulatorMode)
{
    public NachaSimulationMode SimulationMode { get; init; } = NachaSimulationMode.IncomingTransactions;
    public string Environment { get; init; } = "UAT";
    public string? ProfileCode { get; init; }
    public int? DeterministicSeed { get; init; }
    public IReadOnlyList<string> OriginalTraceNumbers { get; init; } = [];
}

/// <summary>
/// I01: contrato independiente para simular el resultado de prenotificaciones
/// realmente originadas por ACH Interbank. Solo se valida su estructura en I01;
/// no habilita generacion, transiciones ni simulacion del reloj.
/// </summary>
public sealed class PrenotificationSimulationContractRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PrenotificationSimulationOutcome Outcome { get; set; }
    public string ClearingHouseCode { get; set; } = string.Empty;
    public IReadOnlyList<int> OriginalPrenotificationIds { get; set; } = [];
    public string OriginalCycleId { get; set; } = string.Empty;
    // Obligatorio solo para Returned. NoReturnByDeadline no representa un ciclo de recepcion.
    public string? ResponseCycleId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public int RespondingFinancialInstitutionId { get; set; }
    // Obligatorio solo para Returned. NoReturnByDeadline no tiene causal.
    public string? ReasonCode { get; set; }
    // Requerido para devoluciones. I02 implementara su reserva/concurrencia.
    public string? IdempotencyKey { get; set; }
}

public sealed record PrenotificationSimulationContractViolation(string Code, string Field, string Message);

/// <summary>ShapeValid no significa elegibilidad operativa; ExecutionEnabled es false en I01.</summary>
public sealed record PrenotificationSimulationContractValidationResponse(
    bool ShapeValid,
    bool ExecutionEnabled,
    string ContractVersion,
    IReadOnlyList<PrenotificationSimulationContractViolation> Violations);

public sealed record PrenotificationSimulationOutcomeCapability(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PrenotificationSimulationOutcome Outcome,
    bool CreatesNachaFile,
    bool RequiresManualNachaUpload,
    bool ExecutionEnabled,
    string Workflow);

public sealed record PrenotificationSimulationContractCapabilitiesResponse(
    string ContractVersion,
    string FileReturnScenario,
    string LegacyScenario,
    IReadOnlyList<string> SupportedClearingHouses,
    bool GeneratedOnly,
    bool AutoImported,
    bool ExternalTransmission,
    IReadOnlyList<PrenotificationSimulationOutcomeCapability> Outcomes);

/// <summary>I02: consulta de prenotificaciones realmente originadas. Solo lectura, incluye no elegibles con motivo.</summary>
public sealed class PrenotificationCandidateQuery
{
    public string ClearingHouseCode { get; set; } = string.Empty;
    public int? RespondingFinancialInstitutionId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record PrenotificationCandidateDto(
    int OriginalPrenotificationId,
    string OriginalCycleId,
    string ClearingHouseCode,
    int RespondingFinancialInstitutionId,
    string RespondingFinancialInstitution,
    string TraceNumber,
    string TransactionExternalId,
    string DestinationAccountMasked,
    DateOnly OriginalBusinessDate,
    string TransactionStatus,
    long? ValidationAttemptId,
    string? ValidationAttemptStatus,
    DateTimeOffset? ResponseDeadlineAtUtc,
    bool TransmittedOrAcknowledged,
    bool HasPriorResponse,
    bool EligibleForReturn,
    bool EligibleForNoReturnByDeadline,
    IReadOnlyList<PrenotificationSimulationContractViolation> Violations);

public sealed record PrenotificationCandidatePage(
    IReadOnlyList<PrenotificationCandidateDto> Items,
    int Page,
    int PageSize,
    int Total,
    bool ExecutionEnabled);

/// <summary>BusinessEligible solo expresa elegibilidad de consulta; NO autoriza generar ni modificar transacciones.</summary>
public sealed record PrenotificationBusinessEligibilityPreviewResponse(
    bool ShapeValid,
    bool BusinessEligible,
    bool ExecutionEnabled,
    string ContractVersion,
    IReadOnlyList<PrenotificationSimulationContractViolation> Violations,
    IReadOnlyList<PrenotificationCandidateDto> Candidates);

/// <summary>I05: reloj virtual limitado a la previsualizacion; jamas se escribe como hora de una transaccion.</summary>
public sealed class PrenotificationTacitAcceptancePreviewRequest
{
    public PrenotificationSimulationContractRequest Contract { get; set; } = new();
    public DateTimeOffset? EvaluationTimeUtc { get; set; }
}

public sealed record PrenotificationTacitAcceptanceItem(
    int OriginalPrenotificationId,
    DateTimeOffset? DeadlineAtUtc,
    bool WouldApplyAtEvaluationTime,
    bool EligibleAtRealTime,
    string Decision,
    IReadOnlyList<PrenotificationSimulationContractViolation> Violations);

public sealed record PrenotificationTacitAcceptancePreviewResponse(
    DateTimeOffset EvaluationTimeUtc,
    DateTimeOffset RealTimeUtc,
    bool IsVirtualTime,
    bool ReadOnly,
    bool ExecutionEnabled,
    IReadOnlyList<PrenotificationTacitAcceptanceItem> Items,
    IReadOnlyList<PrenotificationSimulationContractViolation> Violations);

/// <summary>I05: solo permite confirmar vencimientos REALES en UAT, sin adelantar relojes.</summary>
public sealed record PrenotificationTacitAcceptanceApplyResponse(
    DateTimeOffset RealTimeUtc,
    int Applied,
    int AlreadyApplied,
    IReadOnlyList<PrenotificationTacitAcceptanceItem> Items,
    bool CreatesNachaFile,
    bool ExternalTransmission);

public sealed class InboundSimulationEligibilityPreviewRequest : GenerateNachaInboundSimulationRequest;

public sealed record InboundSimulationEligibilityPreviewResponse(
    bool Eligible,
    string Decision,
    string Message,
    string? FunctionalCode,
    NachaSimulationMode SimulationMode,
    bool GeneratedOnly,
    bool AutoImported,
    bool UploadRequired,
    bool ExternalTransmission);

public sealed class DifferentialResponseTransactionQuery
{
    public string ClearingHouseCode { get; set; } = string.Empty;
    public int? DestinationFinancialInstitutionId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? State { get; set; }
    public string? TransactionType { get; set; }
    public string? TraceNumber { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record DifferentialResponseEligibleTransactionDto(
    int Id,
    string Identifier,
    string TraceNumber,
    string ClearingHouse,
    int DestinationFinancialInstitutionId,
    string DestinationFinancialInstitution,
    string TransactionType,
    DateTime EffectiveDate,
    string Cycle,
    string AccountNumberMasked,
    decimal Amount,
    string State,
    bool HasPriorResponse,
    bool Eligible,
    string? IneligibilityReason);

public sealed record DifferentialResponseTransactionPage(
    IReadOnlyList<DifferentialResponseEligibleTransactionDto> Items,
    int Page,
    int PageSize,
    int Total);

public sealed class AvailableInboundCycleQuery
{
    public string ClearingHouseCode { get; set; } = string.Empty;
    public DateOnly ProcessingDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NachaInboundSimulationType ScenarioType { get; set; } = NachaInboundSimulationType.IncomingCredit;
}

public sealed record AvailableInboundCycleDto(
    string CycleId,
    string CycleCode,
    string CycleName,
    int ClearingHouseId,
    string ClearingHouseCode,
    string ClearingHouseName,
    DateOnly ProcessingDate,
    int TransactionCount,
    string Status);
