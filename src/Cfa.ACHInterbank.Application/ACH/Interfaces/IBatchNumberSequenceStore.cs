namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBatchNumberSequenceStore
{
    Task<BatchNumberRangeReservation> ReserveRangeAsync(
        BatchNumberSequenceScope scope,
        int count,
        CancellationToken ct = default);
}

public sealed record BatchNumberSequenceScope(
    string PolicyCode,
    string ClearingHouseId,
    string OriginatingDfi,
    DateOnly ProcessingDate)
{
    public string ToScopeKey() => $"{ClearingHouseId}|{OriginatingDfi}|{ProcessingDate:yyyy-MM-dd}|{PolicyCode}";
}

public sealed record BatchNumberRangeReservation(
    BatchNumberSequenceScope Scope,
    int PreviousValue,
    int StartValue,
    int EndValue,
    bool WasCreated,
    int ReservedCount,
    int AttemptCount);
