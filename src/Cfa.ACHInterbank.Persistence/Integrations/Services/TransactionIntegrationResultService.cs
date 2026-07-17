using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class TransactionIntegrationResultService : ITransactionIntegrationResultService
{
    private readonly AchDbContext _context;

    public TransactionIntegrationResultService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionIntegrationResultDto?> GetAsync(int transactionId, CancellationToken ct = default)
    {
        var transactionState = await _context.AchTransactions
            .AsNoTracking()
            .Where(x => x.Id == transactionId)
            .Select(x => x.State.ToString())
            .SingleOrDefaultAsync(ct);

        if (transactionState is null)
        {
            return null;
        }

        var contrapartidas = await _context.ContrapartidaDispatchAttempts
            .AsNoTracking()
            .Where(x => x.DispatchItem.AchTransactionId == transactionId)
            .Select(x => new TransactionIntegrationResultItemDto(
                x.ResponseCatalogId,
                x.SoapMethodName,
                x.TransportStatus.ToString(),
                x.BusinessStatus.ToString(),
                x.SoapResponseCode,
                x.SoapResponseDescription,
                x.ProcessedAtUtc,
                x.AttemptNumber,
                x.RetryAllowed,
                x.RequiresManualReview,
                transactionState))
            .ToListAsync(ct);

        var transacciones = await _context.IncomingNachaIntegrationExecution
            .AsNoTracking()
            .Where(x => x.DispatchQueue.AchTransactionId == transactionId)
            .Select(x => new TransactionIntegrationResultItemDto(
                x.ResponseCatalogId,
                x.SoapMethodName,
                x.TransportStatus.ToString(),
                x.BusinessStatus.ToString(),
                x.SoapResponseCode,
                x.SoapResponseDescription,
                x.ProcessedAtUtc,
                x.DispatchQueue.AttemptCount,
                x.RetryAllowed,
                x.RequiresManualReview,
                transactionState))
            .ToListAsync(ct);

        var history = contrapartidas
            .Concat(transacciones)
            .OrderByDescending(x => x.ProcessedAt)
            .ThenByDescending(x => x.AttemptNumber)
            .ToArray();

        return new TransactionIntegrationResultDto(transactionId, history.FirstOrDefault(), history);
    }
}
