namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

public enum ThirdPartyAccountValidationType
{
    Prenotification = 1
}

public enum ThirdPartyAccountValidationStatus
{
    NotStarted = 1,
    PendingSend = 2,
    PendingResponse = 3,
    Accepted = 4,
    TacitlyAccepted = 5,
    Rejected = 6,
    RequiresCorrection = 7,
    TechnicalReview = 8,
    Expired = 9,
    Suspended = 10
}

public enum PrenotificationValidationAttemptStatus
{
    Created = 1,
    Queued = 2,
    Sent = 3,
    AwaitingResponse = 4,
    Accepted = 5,
    TacitlyAccepted = 6,
    Rejected = 7,
    RequiresCorrection = 8,
    TechnicalReview = 9,
    Failed = 10,
    Cancelled = 11
}

public enum PrenotificationValidationResolution
{
    None = 0,
    Accepted = 1,
    TacitlyAccepted = 2,
    Rejected = 3,
    RequiresCorrection = 4,
    TechnicalReview = 5
}

public enum ThirdPartyAccountEligibilityDecision
{
    Allowed = 1,
    AllowedWithoutPrenotification = 2,
    RequiresPrenotification = 3,
    PrenotificationPending = 4,
    ValidationExpired = 5,
    Rejected = 6,
    RequiresRevalidation = 7,
    TechnicalReview = 8,
    Blocked = 9,
    Unsupported = 10
}

public enum PrenotificationResponseDeadlineMode
{
    NotApplicable = 0,
    CycleCount = 1,
    OperationalDayEnd = 2
}
