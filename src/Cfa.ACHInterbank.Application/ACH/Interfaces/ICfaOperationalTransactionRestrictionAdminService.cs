using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICfaOperationalTransactionRestrictionAdminService
{
    Task<IReadOnlyList<CfaOperationalTransactionRestrictionDto>> GetAsync(
        CfaOperationalTransactionRestrictionFilter filter,
        CancellationToken ct = default);

    Task<CfaOperationalTransactionRestrictionDto?> GetByIdAsync(
        int id,
        CancellationToken ct = default);

    Task<CfaOperationalTransactionRestrictionDto> CreateAsync(
        CreateCfaOperationalTransactionRestrictionRequest request,
        CancellationToken ct = default);

    Task<CfaOperationalTransactionRestrictionDto?> UpdateAsync(
        int id,
        UpdateCfaOperationalTransactionRestrictionRequest request,
        CancellationToken ct = default);

    Task<CfaOperationalTransactionRestrictionDto?> SetActiveAsync(
        int id,
        bool isActive,
        ChangeCfaOperationalTransactionRestrictionStatusRequest request,
        CancellationToken ct = default);
}
