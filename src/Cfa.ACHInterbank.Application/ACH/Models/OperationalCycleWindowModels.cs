namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum OperationalCycleWindowStatus
{
    Before = 1,
    Inside = 2,
    After = 3
}

public sealed record OperationalCycleWindow(
    DateTime ProcessingDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string TimeZoneId,
    DateTime LocalStart,
    DateTime LocalEnd,
    DateTimeOffset StartInstant,
    DateTimeOffset EndInstant,
    DateTimeOffset CurrentInstant,
    DateTime LocalNow,
    OperationalCycleWindowStatus Status)
{
    public bool IsInside => Status == OperationalCycleWindowStatus.Inside;
}

