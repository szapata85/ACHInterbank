namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

public enum NachaSimulationMode
{
    IncomingTransactions = 1,
    DifferentialResponses = 2
}

public enum NachaInboundSimulationType
{
    IncomingCredit = 1,
    IncomingDebit = 2,
    IncomingPrenotificationResponse = 3,
    IncomingCreditConfirmation = 4,
    IncomingCreditRejection = 5,
    IncomingCreditReturn = 6,
    IncomingDebitConfirmation = 7,
    IncomingDebitRejection = 8,
    IncomingDebitReturn = 9
}

public enum NachaInboundSimulationStatus
{
    Draft = 1,
    Generated = 2,
    Downloaded = 3,
    Failed = 4,
    Blocked = 5
}

public enum InboundResponseMode
{
    Approved = 1,
    Rejected = 2,
    Confirmed = 3,
    Returned = 4,
    Failed = 5
}
