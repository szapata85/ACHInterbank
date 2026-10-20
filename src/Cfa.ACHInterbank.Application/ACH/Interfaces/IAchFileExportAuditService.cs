namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchFileExportAuditService
{
    Task RecordGeneratedFileAsync(
        string cycleId,
        int clearingHouseId,
        string exportKind,
        string fileName,
        int totalRecords,
        int totalTransactions,
        bool isEncrypted,
        CancellationToken ct = default);

    Task RecordGeneratedFileAsync(
        string cycleId,
        int clearingHouseId,
        string exportKind,
        string fileName,
        int totalRecords,
        int totalTransactions,
        bool isEncrypted,
        IReadOnlyCollection<int> achTransactionIds,
        string? contentSha256,
        CancellationToken ct = default);
}
