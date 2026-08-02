using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchResponseTransactionCorrelationService(AchDbContext context)
    : IAchResponseTransactionCorrelationService
{
    public async Task<AchResponseCorrelationResult> CorrelateAsync(
        string transactionIdentifier,
        CancellationToken cancellationToken = default)
    {
        var identifier = (transactionIdentifier ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return new(AchResponseCorrelationStatus.NotFound, null, "IdentificadorVacio", []);
        }

        var candidates = await context.AchTransactions.AsNoTracking()
            .Where(x => x.TraceNumber == identifier || x.TransactionExternalId == identifier)
            .Select(x => x.Id)
            .Distinct()
            .Take(3)
            .ToListAsync(cancellationToken);

        return candidates.Count switch
        {
            1 => new(AchResponseCorrelationStatus.Matched, candidates[0], "TrazaOIdentificadorExternoExacto", candidates),
            0 => new(AchResponseCorrelationStatus.NotFound, null, "SinCoincidenciaDeterministica", candidates),
            _ => new(AchResponseCorrelationStatus.Ambiguous, null, "MultiplesCoincidenciasExactas", candidates)
        };
    }
}
