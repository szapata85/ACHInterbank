using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ITransactionPolicyService
{
    Task<TransactionPolicyPreview> PreviewAsync(TransactionPolicyPreviewRequest request, CancellationToken ct = default);
}
