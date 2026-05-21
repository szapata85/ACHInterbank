using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class TransactionIntegrationReadinessService : ITransactionIntegrationReadinessService
{
    private readonly AchDbContext _context;
    private readonly ITransactionIntegrationOperationResolver _operationResolver;
    private readonly IIntegrationMappingReadinessService _mappingReadinessService;

    public TransactionIntegrationReadinessService(
        AchDbContext context,
        ITransactionIntegrationOperationResolver operationResolver,
        IIntegrationMappingReadinessService mappingReadinessService)
    {
        _context = context;
        _operationResolver = operationResolver;
        _mappingReadinessService = mappingReadinessService;
    }

    public async Task<TransactionIntegrationReadinessResult?> GetTransactionReadinessAsync(int transactionId, CancellationToken ct = default)
    {
        var transaction = await _context.AchTransactions
            .AsNoTracking()
            .Include(x => x.SourceInstitution)
            .Include(x => x.DestinationInstitution)
            .FirstOrDefaultAsync(x => x.Id == transactionId, ct);

        if (transaction is null)
        {
            return null;
        }

        var operation = await _operationResolver.ResolveAsync(transaction, ct);
        var readiness = await _mappingReadinessService.EvaluateAsync(operation, ct);

        return new TransactionIntegrationReadinessResult(
            transaction.Id,
            operation.Reference,
            operation.IntegrationKey,
            operation.OperationKey,
            operation.MappingPurpose,
            operation.MappingDirection,
            operation.FunctionalNature,
            operation.FunctionalOriginator,
            operation.MovesMoney,
            operation.Reason,
            operation.IsSupported,
            readiness);
    }
}
