using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigProfileQueryService
{
    Task<IReadOnlyList<NachaConfigProfileListItemDto>> GetProfilesAsync(CancellationToken ct = default);
    Task<NachaConfigProfileDetailDto?> GetProfileDetailAsync(int profileId, CancellationToken ct = default);
    Task<NachaConfigFilterCatalogsDto> GetFilterCatalogsAsync(CancellationToken ct = default);
}
