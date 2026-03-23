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
}
