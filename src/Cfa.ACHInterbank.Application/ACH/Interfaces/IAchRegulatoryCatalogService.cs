using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchRegulatoryCatalogService
{
    Task<int> GetPriorityAsync(TransactionTypeEnum transactionType, CancellationToken ct);
    Task<bool> IsPrenotificationRequiredAsync(TransactionTypeEnum transactionType, CancellationToken ct);
    Task<(bool IsAllowed, string? Reason)> ValidateReturnCodeAsync(string returnCode, TransactionTypeEnum transactionType, DateTime originalDate, DateTime currentDate, CancellationToken ct);
    Task<(bool IsAllowed, string? Reason, bool IsUniquePerTransaction)> ValidateReturnOfReturnAsync(string originalReturnCode, string newReturnCode, string originalState, DateTime originalDate, DateTime currentDate, CancellationToken ct);
    Task<AchFileRejectionCode?> ResolveFileRejectionCodeAsync(string stage, string code, CancellationToken ct);
    Task<IReadOnlyList<AchReturnCode>> GetReturnCodesAsync(CancellationToken ct);
    Task<IReadOnlyList<AchFileRejectionCode>> GetFileRejectionCodesAsync(CancellationToken ct);
    Task<IReadOnlyList<AchTransactionTypePolicy>> GetTransactionTypePoliciesAsync(CancellationToken ct);
}
