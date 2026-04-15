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
    private static readonly HashSet<string> AllowedReasonCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "R01", "R02", "R03", "R09", "R10", "D01", "D02", "D03"
    };

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
        if (returnOfReturn.Type != TransactionTypeEnum.Return)
        {
            throw new InvalidOperationException("La devolución de devolución debe tener tipo Return.");
        }
        if (!AllowedReasonCodes.Contains(reasonCode))
        {
            throw new InvalidOperationException($"La causal {reasonCode} no está habilitada para devolución de devolución.");
        }
        if (sourceReturn.State is not (AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr or AchTransferStateEnum.Certified))
        {
            throw new InvalidOperationException("La devolución origen no se encuentra en estado elegible para devolución de devolución.");
        }
        if (sourceReturn.SlaDeadlineAtUtc.HasValue && DateTime.UtcNow > sourceReturn.SlaDeadlineAtUtc.Value)
        {
            throw new InvalidOperationException("El plazo operativo para devolución de devolución expiró.");
        }

        var duplicated = await _context.ReturnOfReturnFlows
            .AnyAsync(x => x.SourceReturnTransactionId == sourceReturn.Id
                           || x.ReturnOfReturnTransactionId == returnOfReturn.Id, ct);
        if (duplicated)
        {
            throw new InvalidOperationException("Ya existe un flujo de devolución de devolución para la transacción indicada.");
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
