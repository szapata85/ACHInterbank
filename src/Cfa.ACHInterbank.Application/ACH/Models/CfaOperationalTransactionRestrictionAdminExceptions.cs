namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class CfaOperationalTransactionRestrictionValidationException(string message)
    : InvalidOperationException(message);

public sealed class CfaOperationalTransactionRestrictionConcurrencyException(
    string message,
    string? currentRowVersion = null) : InvalidOperationException(message)
{
    public string? CurrentRowVersion { get; } = currentRowVersion;
}
