using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public record TransactionBatchContext
{
    public AchBatch Batch { get; init; } = null!;
    public string AchCycleId { get; init; } = string.Empty;
    public DateTime EffectiveEntryDate { get; init; }
    public string OriginatingDfi { get; init; } = string.Empty;
    public string ReceivingDfi { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyIdentification { get; init; } = string.Empty;
    public string CompanyEntryDescription { get; init; } = string.Empty;
    public int CompanyEntryDescriptionId { get; init; }
    public DateTime? ReturnSlaDeadlineAtUtc { get; init; }
    public string ServiceClassCode { get; init; } = "200";
    public int SourceInstitutionId { get; init; }
    public int DestinationInstitutionId { get; init; }
}
