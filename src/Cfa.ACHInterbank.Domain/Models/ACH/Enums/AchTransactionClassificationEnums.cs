namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

public enum AchTransactionDirection
{
    Unknown = 0,
    Outgoing = 1,
    Incoming = 2
}

public enum AchTransactionOrigin
{
    Unknown = 0,
    Cfa = 1,
    ExternalInstitution = 2
}

public enum AchMonetaryIntegrationRoute
{
    None = 0,
    ProcContrapartidas = 1,
    ProcTransacciones = 2,
    ManualReview = 3
}

public enum AchTransactionClassificationStatus
{
    Unknown = 0,
    Determined = 1,
    Ambiguous = 2,
    Invalid = 3
}
