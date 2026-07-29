using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseTransactionRuleService
{
    Task<IReadOnlyList<ClearingHouseTransactionRuleDto>> GetAsync(int? clearingHouseId, string? transactionNature, bool includeInactive, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> CreateAsync(CreateClearingHouseTransactionRuleRequest request, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> UpdateAsync(int id, UpdateClearingHouseTransactionRuleRequest request, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> SetActiveAsync(int id, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<ClearingHouseTransactionRuleDto>> GetVersionsAsync(int clearingHouseId, TransactionTypeEnum? transactionType, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto?> GetCurrentAsync(int clearingHouseId, TransactionTypeEnum transactionType, DateTime effectiveAt, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto?> GetByIdAsync(int clearingHouseId, int id, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> CreateVersionAsync(int clearingHouseId, CreateClearingHouseTransactionPolicyVersionRequest request, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> UpdateMetadataAsync(int clearingHouseId, int id, UpdateClearingHouseTransactionPolicyMetadataRequest request, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> CloseVersionAsync(int clearingHouseId, int id, CloseClearingHouseTransactionPolicyVersionRequest request, CancellationToken ct);
    Task<ClearingHouseTransactionRuleDto> ActivateVersionAsync(int clearingHouseId, int id, CancellationToken ct);
}
