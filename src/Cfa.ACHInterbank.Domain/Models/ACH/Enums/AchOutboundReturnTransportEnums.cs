namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

public enum AchFileTransmissionAttemptStatus
{
    Started = 0,
    Succeeded = 1,
    FailedRetryable = 2,
    FailedFinal = 3
}

public enum AchOutboundReturnOutcome
{
    Acknowledged = 1,
    Accepted = 2,
    Rejected = 3,
    Unknown = 4
}
