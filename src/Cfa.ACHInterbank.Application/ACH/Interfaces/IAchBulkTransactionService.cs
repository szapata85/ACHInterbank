using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchBulkTransactionService
{
    Task<BulkAchTransactionResponse> RegisterBulkAsync(BulkAchTransactionRequest request, CancellationToken ct = default);
}
