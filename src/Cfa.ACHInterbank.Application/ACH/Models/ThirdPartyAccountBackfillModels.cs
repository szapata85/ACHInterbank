namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ThirdPartyAccountBackfillRequest(
    bool DryRun = true,
    int? AfterLegacyThirdPartyId = null,
    int BatchSize = 250);

public sealed record ThirdPartyAccountBackfillIssue(
    int LegacyThirdPartyId,
    string Category,
    string Code);

public sealed record ThirdPartyAccountBackfillSummary(
    bool DryRun,
    int? StartAfterLegacyThirdPartyId,
    int? LastProcessedLegacyThirdPartyId,
    bool HasMore,
    int Scanned,
    int Created,
    int Reused,
    int SkippedIncomplete,
    int Ambiguous,
    int Failed,
    int ValidationHistoryCreated,
    int ValidationHistoryReused,
    int ValidationHistorySkippedIncomplete,
    int ValidationHistoryAmbiguous,
    int ValidationHistoryFailed,
    IReadOnlyList<ThirdPartyAccountBackfillIssue> Issues);
