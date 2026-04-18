using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigProfileCommandService
{
    Task<NachaConfigProfileDetailDto> CreateDraftAsync(NachaConfigCreateDraftRequest request, string actor, CancellationToken ct = default);
    Task<NachaConfigProfileDetailDto?> UpdateDraftAsync(int profileId, NachaConfigUpdateProfileRequest request, string actor, CancellationToken ct = default);
    Task<NachaConfigProfileDetailDto?> CloneProfileAsync(int profileId, NachaConfigCloneProfileRequest request, string actor, CancellationToken ct = default);
    Task<bool> InactivateProfileAsync(int profileId, string actor, string expectedRowVersion, CancellationToken ct = default);
    Task<bool> ArchiveProfileAsync(int profileId, string actor, string expectedRowVersion, CancellationToken ct = default);
    Task<bool> UpdateRecordSequenceAsync(int profileId, NachaConfigRecordSequenceUpdateRequest request, string actor, CancellationToken ct = default);
    Task<bool> UpdateLayoutVariantAsync(int profileId, int variantId, NachaConfigLayoutVariantEditDto request, string actor, CancellationToken ct = default);
    Task<bool> UpdateLayoutFieldAsync(int profileId, int fieldId, NachaConfigLayoutFieldEditDto request, string actor, CancellationToken ct = default);
    Task<bool> UpdateFieldRuleAsync(int profileId, int ruleId, NachaConfigFieldRuleEditDto request, string actor, CancellationToken ct = default);
}
