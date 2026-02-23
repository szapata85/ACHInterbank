namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaFileIdentifierMapService
{
    Task<char> ResolveIdentifierAsync(int sequence, CancellationToken ct = default);
}
