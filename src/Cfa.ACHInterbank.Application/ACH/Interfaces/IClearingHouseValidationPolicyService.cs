using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseValidationPolicyService
{
    Task<ClearingHouseValidationPolicyResolution> ResolveAsync(
        int clearingHouseId,
        TransactionNature transactionNature,
        DateTime effectiveDate,
        CancellationToken ct = default);

    Task<DateTimeOffset?> ResolvePrenotificationResponseDeadlineAsync(
        ClearingHouseTransactionRule rule,
        string sourceCycleId,
        CancellationToken ct = default);

    DateTime AddBusinessDays(DateTime start, int days);
}
