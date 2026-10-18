namespace Cfa.ACHInterbank.Domain.Models.ACH.Claims;

/// <summary>
/// Lifecycle interno mínimo de un caso Claims. No representa un catálogo normativo de ACH Colombia.
/// </summary>
public enum ClaimCaseStatus
{
    Open = 1,
    Resolved = 2,
    Expired = 3
}
