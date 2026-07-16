namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaGenerationException : InvalidOperationException
{
    public NachaGenerationException(string code, string message)
        : base($"{code}: {message}")
    {
        Code = code;
    }

    public string Code { get; }
}
