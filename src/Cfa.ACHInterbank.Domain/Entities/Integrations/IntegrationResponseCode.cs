using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Entities.Integrations;

public static class IntegrationResponseCategory
{
    public const string CoreSoapResponse = "CORE_SOAP_RESPONSE";
    public const string AchReturnCause = "ACH_RETURN_CAUSE";
    public const string AchOperatorReturn = "ACH_OPERATOR_RETURN";
    public const string AchFatalFileError = "ACH_FATAL_FILE_ERROR";
    public const string AchClaimCause = "ACH_CLAIM_CAUSE";
}

public enum IntegrationResponseBusinessStatus
{
    Success = 1,
    Rejected = 2,
    PendingCatalog = 3,
    ManualReview = 4,
    Unknown = 5
}

public enum IntegrationTransportStatus
{
    Succeeded = 1,
    Failed = 2,
    TimedOut = 3,
    NotExecuted = 4
}

public sealed class IntegrationResponseCode : AuditableEntity
{
    public long Id { get; set; }
    public int MethodId { get; set; }
    public IntegrationMethod Method { get; set; } = null!;

    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IntegrationResponseBusinessStatus BusinessStatus { get; set; }
    public bool RetryAllowed { get; set; }
    public bool RequiresManualReview { get; set; }
    public string TargetTransactionState { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
}
