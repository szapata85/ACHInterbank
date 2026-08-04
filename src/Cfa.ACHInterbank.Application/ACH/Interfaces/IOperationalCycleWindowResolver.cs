using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IOperationalCycleWindowResolver
{
    OperationalCycleWindow Resolve(
        DateTime processingDate,
        TimeSpan startTime,
        TimeSpan endTime,
        string timeZoneId,
        DateTimeOffset currentInstant);

    DateTimeOffset ConvertLocalToInstant(DateTime localDateTime, string timeZoneId);
}

