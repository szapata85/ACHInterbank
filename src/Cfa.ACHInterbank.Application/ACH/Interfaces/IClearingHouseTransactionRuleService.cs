using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseTransactionRuleService
{
    Task<IReadOnlyList<ClearingHouseTransactionRuleDto>> GetAsync(int? clearingHouseId, string? transactionNature, bool includeInactive, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> CreateAsync(CreateClearingHouseTransactionRuleRequest request, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> UpdateAsync(int id, UpdateClearingHouseTransactionRuleRequest request, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> SetActiveAsync(int id, bool isActive, CancellationToken ct);
}
