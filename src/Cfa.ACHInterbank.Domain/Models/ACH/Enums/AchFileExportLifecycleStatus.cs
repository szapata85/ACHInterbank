namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

public enum AchFileExportLifecycleStatus
{
    HistoricalUnknown = 0,
    Generated = 1,
    Validated = 2,
    Signed = 3,
    Protected = 4,
    AvailableForDelivery = 5,
    Transmitted = 6,
    Acknowledged = 7,
    Accepted = 8,
    Rejected = 9
}

public enum AchResponseCorrelationStatus
{
    Unknown = 0,
    Matched = 1,
    NotFound = 2,
    Ambiguous = 3,
    ManualReviewRequired = 4
}
