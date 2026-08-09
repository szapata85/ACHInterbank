using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class IncomingNachaClassificationResult
{
    public IncomingNachaFunctionalClass FunctionalClass { get; init; }
    public IncomingNachaEligibilityStatus EligibilityStatus { get; init; }
    public bool RequiresLink { get; init; }
    public bool RequiresManualResolution { get; init; }
    public string? OriginalTraceRef { get; init; }
    public string? ReturnReasonCode { get; init; }
    public IncomingNachaPrenoteStatus PrenoteStatus { get; init; }
    public string BusinessMeaning { get; init; } = string.Empty;
    public string ClassifierVersion { get; init; } = "v1.1.0";
    public string ClassificationEvidenceJson { get; init; } = "{}";
}

public sealed class IncomingNachaLinkingResult
{
    public IncomingNachaLinkType LinkType { get; init; } = IncomingNachaLinkType.NoResuelto;
    public int? AchTransactionId { get; init; }
    public bool IsFinal { get; init; }
    public decimal ConfidenceScore { get; init; }
    public string EvidenceJson { get; init; } = "{}";
    public bool IsAmbiguous { get; init; }
    public bool IsNotFound { get; init; }
}

public sealed class IncomingNachaLinkingContext
{
    public Guid IncomingNachaFileIngestionId { get; init; }
    public string? ResolvedAchCycleId { get; init; }
    public int? ResolvedClearingHouseId { get; init; }
    public DateTime? OperationalDate { get; init; }
    public IncomingNachaFunctionalClass FunctionalClass { get; init; }
}

public sealed record IncomingNachaLinkedReturnApplicationResult(
    bool Applied,
    bool WasDuplicate,
    bool RequiresManualResolution,
    long? AchTransactionStateEventId,
    string Status,
    string Message);
