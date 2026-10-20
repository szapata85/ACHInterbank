using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchRegulatoryCatalogService
{
    Task<int> GetPriorityAsync(TransactionTypeEnum transactionType, CancellationToken ct);
    Task<bool> IsPrenotificationRequiredAsync(TransactionTypeEnum transactionType, CancellationToken ct);
    Task<(bool IsAllowed, string? Reason)> ValidateReturnCodeAsync(int clearingHouseId, string returnCode, TransactionTypeEnum transactionType, DateTime originalDate, DateTime currentDate, CancellationToken ct);
    Task<(bool IsAllowed, string? Reason)> ValidateReturnPolicyAsync(int clearingHouseId, TransactionTypeEnum transactionType, string returnCode, DateTime originalDate, DateTime currentDate, bool hasAddenda, string originalState, CancellationToken ct);
    Task<(bool IsAllowed, string? Reason, bool IsUniquePerTransaction)> ValidateReturnOfReturnAsync(int clearingHouseId, string originalReturnCode, string newReturnCode, string originalState, DateTime originalDate, DateTime currentDate, CancellationToken ct);
    Task<AchFileRejectionCode?> ResolveFileRejectionCodeAsync(string stage, string code, CancellationToken ct);
    Task<AchFileRejectionCode?> ResolveFileRejectionCodeAsync(int? clearingHouseId, string stage, string code, DateTime effectiveDate, CancellationToken ct);
    Task<IReadOnlyList<AchReturnCode>> GetReturnCodesAsync(CancellationToken ct);
    Task<IReadOnlyList<AchReturnCode>> GetReturnCodesAsync(int? clearingHouseId, CancellationToken ct);
    Task<IReadOnlyList<AchReturnCode>> GetReturnCodesByClearingHouseCodeAsync(string clearingHouseCode, CancellationToken ct);
    Task<IReadOnlyList<AchFileRejectionCode>> GetFileRejectionCodesAsync(CancellationToken ct);
    Task<IReadOnlyList<AchTransactionTypePolicy>> GetTransactionTypePoliciesAsync(CancellationToken ct);
    Task<IReadOnlyList<AchReturnPolicy>> GetReturnPoliciesAsync(CancellationToken ct);
    Task<IReadOnlyList<AchReturnOfReturnPolicy>> GetReturnOfReturnPoliciesAsync(CancellationToken ct);
    Task<IReadOnlyList<AchPrenotificationPolicy>> GetPrenotificationPoliciesAsync(CancellationToken ct);
}
