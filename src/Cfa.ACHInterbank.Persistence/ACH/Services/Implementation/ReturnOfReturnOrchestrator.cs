using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ReturnOfReturnOrchestrator : IReturnOfReturnOrchestrator
{
    private readonly AchDbContext _context;

    public ReturnOfReturnOrchestrator(AchDbContext context)
    {
        _context = context;
    }

    public async Task<ReturnOfReturnFlow> RegisterAsync(AchTransaction sourceReturn, AchTransaction returnOfReturn, string reasonCode, CancellationToken ct)
    {
        if (sourceReturn.Type != TransactionTypeEnum.Return)
        {
            throw new InvalidOperationException("La transacción origen debe ser una devolución para registrar devolución de devolución.");
        }

        var latestExecutionId = await _context.CenitCycleExecutions
            .Where(x => x.AchCycleId == returnOfReturn.AchCycleId)
            .OrderByDescending(x => x.Id)
            .Select(x => (long?)x.Id)
            .FirstOrDefaultAsync(ct);

        var flow = new ReturnOfReturnFlow
        {
            SourceReturnTransactionId = sourceReturn.Id,
            ReturnOfReturnTransactionId = returnOfReturn.Id,
            ReasonCode = reasonCode,
            Status = "Registered",
            OrchestratedAtUtc = DateTime.UtcNow,
            CenitCycleExecutionId = latestExecutionId
        };

        _context.ReturnOfReturnFlows.Add(flow);
        await _context.SaveChangesAsync(ct);
        return flow;
    }
}
