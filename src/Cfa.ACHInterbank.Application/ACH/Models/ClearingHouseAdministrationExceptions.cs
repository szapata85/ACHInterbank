namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class ClearingHouseValidationException : Exception
{
    public ClearingHouseValidationException(string message, IReadOnlyList<string>? missingRequirements = null)
        : base(message)
    {
        MissingRequirements = missingRequirements ?? [];
    }

    public IReadOnlyList<string> MissingRequirements { get; }
}

public sealed class ClearingHouseConflictException : Exception
{
    public ClearingHouseConflictException(string message) : base(message) { }
}

public sealed class ClearingHouseNotFoundException : Exception
{
    public ClearingHouseNotFoundException() : base("La cámara compensadora no existe.") { }
}
