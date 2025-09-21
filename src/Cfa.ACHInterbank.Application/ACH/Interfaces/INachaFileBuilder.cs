namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaFileBuilder
{
    Task<byte[]> BuildNachaFileAsync(int achCycleId, CancellationToken ct = default);
}

