using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigProfileQueryService
{
    Task<IReadOnlyList<NachaConfigProfileListItemDto>> GetProfilesAsync(CancellationToken ct = default);
    Task<NachaConfigProfileDetailDto?> GetProfileDetailAsync(int profileId, CancellationToken ct = default);
    Task<NachaConfigWorkspaceDto?> GetWorkspaceAsync(int profileId, CancellationToken ct = default);
    Task<NachaConfigFieldEditorDto?> GetFieldEditorAsync(int profileId, int fieldId, CancellationToken ct = default);
    Task<NachaConfigFilterCatalogsDto> GetFilterCatalogsAsync(CancellationToken ct = default);
    Task<NachaConfigFilterCatalogsDto> GetOperationalCatalogsAsync(
        string? camaraCode = null,
        string? flujoCode = null,
        CancellationToken ct = default);
}
