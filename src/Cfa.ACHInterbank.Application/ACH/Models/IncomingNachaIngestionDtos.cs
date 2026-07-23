using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class IncomingNachaIngestionRequest
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
    public string RequestedBy { get; init; } = "system";
    public string CorrelationId { get; init; } = string.Empty;
    public Guid? ParentIngestionId { get; init; }
    public bool ForceReprocess { get; init; }
}

public sealed class IncomingNachaIngestionResponse
{
    public Guid IngestionId { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string FileHash { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public IncomingNachaIngestionStatus IngestionStatus { get; init; }
    public IncomingNachaCycleResolutionStatus CycleResolutionStatus { get; init; }
    public IncomingNachaParsingStatus ParsingStatus { get; init; }
    public int? DetectedClearingHouseId { get; init; }
    public int? ResolvedClearingHouseId { get; init; }
    public string? ResolvedAchCycleId { get; init; }
    public DateTime? OperationalDate { get; init; }
    public NachaProfileSelectionStatus? ProfileSelectionStatus { get; init; }
    public string? SelectedProfileCode { get; init; }
    public string? SelectedProfileVersion { get; init; }
    public int TotalBatches { get; init; }
    public int TotalEntries { get; init; }
    public int TotalAddendas { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public sealed class IncomingNachaCycleResolutionRequest
{
    public required string FileName { get; init; }
    public required IReadOnlyList<string> Records { get; init; }
}

public sealed class IncomingNachaCycleResolutionResult
{
    public bool IsResolved { get; init; }
    public bool IsAmbiguous { get; init; }
    public int? ClearingHouseId { get; init; }
    public DateTime? OperationalDate { get; init; }
    public string? AchCycleId { get; init; }
    public decimal Confidence { get; init; }
    public IncomingNachaCycleResolutionStatus Status { get; init; }
    public string ResolutionMode { get; init; } = string.Empty;
    public string EvidenceJson { get; init; } = "{}";
    public int? DetectedClearingHouseId { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public sealed class NachaParseRequest
{
    public Guid? IncomingNachaFileIngestionId { get; init; }
    public int? ResolvedClearingHouseId { get; init; }
    public string? ResolvedAchCycleId { get; init; }
    public DateTime? OperationalDate { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

public sealed class NachaParseResult
{
    public IReadOnlyList<NachaValidationFailure> Failures { get; init; } = [];
    public int TotalBatches { get; init; }
    public int TotalEntries { get; init; }
    public int TotalAddendas { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
    public string? NachaId { get; init; }
}
