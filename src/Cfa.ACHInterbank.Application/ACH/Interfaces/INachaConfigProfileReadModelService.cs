using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigProfileReadModelService
{
    Task<NachaConfigProfilesDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NachaConfigProfileReadModel>> GetProfilesAsync(CancellationToken cancellationToken = default);
    Task<NachaConfigProfileDetailReadModel?> GetProfileAsync(int profileId, CancellationToken cancellationToken = default);
    Task<NachaConfigProfileDetailReadModel?> GetProfileByCodeAsync(string profileCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NachaConfigProfileVariantReadModel>> GetVariantsAsync(int profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NachaConfigProfileFieldReadModel>> GetFieldsAsync(int profileId, CancellationToken cancellationToken = default);
}
