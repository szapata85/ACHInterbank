namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record IncomingProcTransaccionesE2eScenarioRequest
{
    public DateTime OperationalDate { get; init; }
    public int CycleNumber { get; init; } = 1;
}

public sealed record IncomingProcTransaccionesE2eScenarioResult
{
    public bool IsReady { get; init; }
    public bool SetupAuthorized { get; init; }
    public bool CreatedExternalInstitution { get; init; }
    public bool CreatedTransaction { get; init; }
    public int CfaInstitutionId { get; init; }
    public int ExternalInstitutionId { get; init; }
    public int TransactionId { get; init; }
    public string AchCycleId { get; init; } = string.Empty;
    public string ReceivingDfi { get; init; } = string.Empty;
    public string ExternalOriginRouting { get; init; } = string.Empty;
    public string ReceiverAccountMasked { get; init; } = string.Empty;
    public decimal AuthorizedAmount { get; init; }
    public string TransactionExternalId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
