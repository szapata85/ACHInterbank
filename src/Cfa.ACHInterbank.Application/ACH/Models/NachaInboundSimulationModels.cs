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
