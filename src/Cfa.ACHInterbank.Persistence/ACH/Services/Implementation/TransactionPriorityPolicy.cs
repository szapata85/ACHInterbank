using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionPriorityPolicy : ITransactionPriorityPolicy
{
    private readonly IAchRegulatoryCatalogService _catalogService;

    public TransactionPriorityPolicy(IAchRegulatoryCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<int> ResolvePriorityAsync(AchTransaction transaction, CancellationToken ct)
    {
        var catalogPriority = await _catalogService.GetPriorityAsync(transaction.Type, ct);
        if (catalogPriority > 0)
        {
            return catalogPriority;
        }

        return 10;
    }
}
