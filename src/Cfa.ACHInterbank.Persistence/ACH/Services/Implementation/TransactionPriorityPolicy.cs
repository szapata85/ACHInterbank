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

    public int ResolvePriority(AchTransaction transaction)
    {
        var catalogPriority = _catalogService.GetPriorityAsync(transaction.Type, CancellationToken.None).GetAwaiter().GetResult();
        if (catalogPriority > 0)
        {
            return catalogPriority;
        }

        return 10;
    }
}
