namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

public enum CenitChamberResponseType
{
    Unknown = 0,
    Ack = 1,
    Nack = 2,
    OperatorRejected = 3,
    Reconciliation = 4,
    NoActivity = 5
}

public enum CenitChamberResponseState
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    OperatorRejected = 3,
    Reconciliation = 4,
    NoActivity = 5
}

public enum CenitChamberCorrelationOutcome
{
    Pending = 0,
    Matched = 1,
    NotFound = 2,
    Ambiguous = 3,
    TransactionNotFound = 4,
    TransactionAmbiguous = 5,
    Invalid = 6,
    InvalidTransition = 7
}
