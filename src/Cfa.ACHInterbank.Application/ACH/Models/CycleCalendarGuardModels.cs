namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CycleCalendarGuardResult(
    bool CanExecute,
    bool WasDeferred,
    DateOnly EvaluatedDate,
    DateOnly? RescheduledDate,
    string Reason);

public sealed class CycleDeferredByCalendarException : InvalidOperationException
{
    public CycleDeferredByCalendarException(string cycleId, CycleCalendarGuardResult result)
        : base($"El ciclo {cycleId} fue diferido por calendario operativo: {result.Reason}")
    {
        CycleId = cycleId;
        Result = result;
    }

    public string CycleId { get; }
    public CycleCalendarGuardResult Result { get; }
}
