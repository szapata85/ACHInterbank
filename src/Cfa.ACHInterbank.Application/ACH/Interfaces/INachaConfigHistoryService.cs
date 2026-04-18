using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigHistoryService
{
    Task<IReadOnlyList<NachaConfigHistoryItemDto>> GetHistoryAsync(int profileId, CancellationToken ct = default);
    Task<IReadOnlyList<NachaConfigSnapshotItemDto>> GetSnapshotsAsync(int profileId, CancellationToken ct = default);
}
