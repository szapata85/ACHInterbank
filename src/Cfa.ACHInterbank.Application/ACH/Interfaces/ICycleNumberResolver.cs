namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICycleNumberResolver
{
    int? Resolve(string? cycleName);
}

