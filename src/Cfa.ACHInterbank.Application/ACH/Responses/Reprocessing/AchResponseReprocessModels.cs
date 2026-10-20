namespace Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;

public static class AchResponseReprocessAttemptStatuses
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string FailedFunctional = "FailedFunctional";
    public const string FailedTechnical = "FailedTechnical";
}

public enum AchResponseReprocessResultCode
{
    Completed,
    MappingNotFound,
    MappingAmbiguous,
    CorrelationNotFound,
    MissingOperationalData,
    LostOwnership,
    AlreadyApplied,
    TechnicalFailure
}

public sealed record AchResponseReprocessExecutionResult(
    AchResponseReprocessResultCode Code, string Result, string? ErrorDetailSanitized = null)
{
    public bool IsTechnicalFailure => Code == AchResponseReprocessResultCode.TechnicalFailure;
    public bool RequiresManualReview => Code is AchResponseReprocessResultCode.MappingNotFound
        or AchResponseReprocessResultCode.MappingAmbiguous or AchResponseReprocessResultCode.CorrelationNotFound
        or AchResponseReprocessResultCode.MissingOperationalData;
}

public sealed record AchResponseReprocessDispatchResult(int Candidates, int Claimed, int Completed,
    int FailedFunctional, int FailedTechnical, int Skipped)
{
    public string Summary => $"candidates={Candidates}; claimed={Claimed}; completed={Completed}; functional={FailedFunctional}; technical={FailedTechnical}; skipped={Skipped}";
}
