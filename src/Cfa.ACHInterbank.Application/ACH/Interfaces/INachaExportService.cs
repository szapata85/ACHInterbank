namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaExportService
{
    Task<NachaExportResult> ExportAsync(string cycleId, CancellationToken ct = default);
    Task<NachaExportResult> ExportEncryptedAsync(string cycleId, bool forceEncryption = false, CancellationToken ct = default);
}
