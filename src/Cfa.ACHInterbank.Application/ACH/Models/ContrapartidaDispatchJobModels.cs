namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ContrapartidaCycleDispatchResult(
    string CycleId,
    int ClearingHouseId,
    int Processed,
    int Succeeded,
    int Failed,
    int Partial,
    int Chunks,
    string Summary);
