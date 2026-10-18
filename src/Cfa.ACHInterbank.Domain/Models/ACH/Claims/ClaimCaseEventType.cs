namespace Cfa.ACHInterbank.Domain.Models.ACH.Claims;

/// <summary>
/// Tipos mínimos de historial del caso Claims. Los avances no son estados del caso.
/// </summary>
public enum ClaimCaseEventType
{
    Opened = 1,
    Progress = 2,
    Resolved = 3,
    Expired = 4
}
