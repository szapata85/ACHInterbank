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
    IncomingDebitReturn = 9,
    // I01: nuevo escenario explicito para una devolucion de prenotificacion.
    // El valor 3 (IncomingPrenotificationResponse) es legado y no debe reasignarse.
    // No habilitar generacion hasta homologacion I02/I03.
    IncomingPrenotificationReturn = 10
}

/// <summary>
/// El resultado de una prenotificacion saliente NO implica siempre un archivo recibido.
/// </summary>
public enum PrenotificationSimulationOutcome
{
    Returned = 1,
    NoReturnByDeadline = 2
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
