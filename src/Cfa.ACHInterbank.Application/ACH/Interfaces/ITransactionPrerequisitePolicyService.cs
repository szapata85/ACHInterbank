using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ITransactionPrerequisitePolicyService
{
    Task<TransactionPrerequisitePreviewResponse> PreviewAsync(TransactionPrerequisitePreviewRequest request, CancellationToken ct);
    Task<TransactionPrerequisiteValidationResult> ValidateForNachaExportAsync(AchTransaction transaction, DateTime? prenotificationDate, CancellationToken ct);
}

public sealed record TransactionPrerequisiteValidationResult(
    bool IsValid,
    string Code,
    string Message,
    ClearingHouseTransactionRule? Rule);
