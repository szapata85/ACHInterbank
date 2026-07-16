using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IOperationalTimeSnapshotProvider
{
    OperationalTimeSnapshot CaptureNow();

    OperationalTimeSnapshot GetOrCreate(
        string operationKey,
        DateOnly operationalDate,
        TimeOnly preferredFileCreationTime);
}
