namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class AchColombiaCycleOperationalScheduleValidationException(string message)
    : InvalidOperationException(message);

public sealed class AchColombiaCycleOperationalScheduleConcurrencyException(
    string message,
    string? currentRowVersion = null) : InvalidOperationException(message)
{
    public string? CurrentRowVersion { get; } = currentRowVersion;
}
