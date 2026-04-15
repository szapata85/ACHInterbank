using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CenitNettingService : ICenitNettingService
{
    private readonly AchDbContext _context;

    public CenitNettingService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<CenitNettingExecution> CalculateAsync(CenitCycleExecution execution, CancellationToken ct)
    {
        var txs = await _context.AchTransactions
            .AsNoTracking()
            .Where(x => x.AchCycleId == execution.AchCycleId)
            .ToListAsync(ct);

        var netting = new CenitNettingExecution
        {
            CenitCycleExecutionId = execution.Id,
            CalculatedAtUtc = DateTime.UtcNow,
            TotalDebit = txs.Sum(x => x.Amount),
            TotalCredit = txs.Sum(x => x.Amount)
        };

        var grouped = txs
            .SelectMany(tx => new[]
            {
                new { FinancialInstitutionId = tx.SourceInstitutionId, Debit = tx.Amount, Credit = 0m },
                new { FinancialInstitutionId = tx.DestinationInstitutionId, Debit = 0m, Credit = tx.Amount }
            })
            .GroupBy(x => x.FinancialInstitutionId)
            .Select(g => new CenitNetPosition
            {
                FinancialInstitutionId = g.Key,
                DebitAmount = g.Sum(x => x.Debit),
                CreditAmount = g.Sum(x => x.Credit),
                NetAmount = g.Sum(x => x.Credit) - g.Sum(x => x.Debit),
                AvailableLiquidity = g.Sum(x => x.Credit),
                HasInsufficientFunds = g.Sum(x => x.Credit) - g.Sum(x => x.Debit) < 0
            })
            .ToList();

        var details = txs.Select(tx => new CenitNettingDetail
        {
            AchTransactionId = tx.Id,
            SourceInstitutionId = tx.SourceInstitutionId,
            DestinationInstitutionId = tx.DestinationInstitutionId,
            Amount = tx.Amount,
            IncludedInSettlement = true,
            DecisionReason = "IncludedInMultilateralNetting"
        }).ToList();

        netting.NetPositions = grouped;
        netting.Details = details;

        _context.CenitNettingExecutions.Add(netting);
        await _context.SaveChangesAsync(ct);
        return netting;
    }
}
