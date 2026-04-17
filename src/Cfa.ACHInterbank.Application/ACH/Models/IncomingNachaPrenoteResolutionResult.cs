using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class IncomingNachaPrenoteResolutionResult
{
    public IncomingNachaPrenoteStatus PrenoteStatus { get; init; } = IncomingNachaPrenoteStatus.RequiereRevision;
    public bool Applied { get; init; }
    public bool RequiresManualReview { get; init; }
    public string EvidenceJson { get; init; } = "{}";
    public string Message { get; init; } = string.Empty;
}
