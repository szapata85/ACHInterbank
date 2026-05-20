using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public class GenerateNachaInboundSimulationRequest
{
    public string ClearingHouseCode { get; set; } = string.Empty;
    public NachaInboundSimulationType ScenarioType { get; set; }
    public string? OriginFinancialInstitutionCode { get; set; }
    public string? DestinationFinancialInstitutionCode { get; set; }
    public int EntriesCount { get; set; } = 1;
    public decimal Amount { get; set; } = 1000m;
    public string ReferencePrefix { get; set; } = "UAT-IN";
    public DateOnly BusinessDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string CycleCode { get; set; } = "Ciclo 3";
    public IReadOnlyList<string> PendingPrenotificationReferences { get; set; } = [];
    public IReadOnlyList<string> TransactionReferences { get; set; } = [];
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
    string SimulatorMode);

public sealed class InboundSimulationEligibilityPreviewRequest : GenerateNachaInboundSimulationRequest;

public sealed record InboundSimulationEligibilityPreviewResponse(
    bool Eligible,
    string Decision,
    string Message,
    string? FunctionalCode,
    bool GeneratedOnly,
    bool AutoImported,
    bool UploadRequired,
    bool ExternalTransmission);
