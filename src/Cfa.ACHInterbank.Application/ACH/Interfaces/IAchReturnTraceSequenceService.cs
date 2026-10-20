namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchReturnTraceSequenceService
{
    Task<AchReturnTraceRange> ReserveRangeAsync(
        string participantDfi,
        DateOnly sequenceDate,
        int count,
        DateTime capturedAtUtc,
        CancellationToken ct = default);
}

public sealed record AchReturnTraceRange(int StartValue, int EndValue)
{
    public int Count => EndValue - StartValue + 1;
}
