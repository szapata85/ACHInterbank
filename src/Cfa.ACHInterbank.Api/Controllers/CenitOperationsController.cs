using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/cenit")]
[Authorize]
public class CenitOperationsController : ControllerBase
{
    private readonly AchDbContext _dbContext;

    public CenitOperationsController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("queues")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetQueueAsync(
        [FromQuery] string? status,
        [FromQuery] string? targetAchCycleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _dbContext.CenitCycleQueues
            .AsNoTracking()
            .Include(x => x.AchTransaction)
            .Include(x => x.TargetAchCycle)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(targetAchCycleId))
        {
            query = query.Where(x => x.TargetAchCycleId == targetAchCycleId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.EnqueuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.QueueReason,
                x.EnqueuedAtUtc,
                x.DequeuedAtUtc,
                x.TargetAchCycleId,
                TargetCycleName = x.TargetAchCycle.CycleName,
                x.OriginalAchCycleId,
                TransactionId = x.AchTransactionId,
                TransactionExternalId = x.AchTransaction.TransactionExternalId,
                x.AchTransaction.Reference,
                x.AchTransaction.Amount,
                TransactionType = x.AchTransaction.Type.ToString(),
                TransactionState = x.AchTransaction.State.ToString(),
                x.AchTransaction.EffectiveEntryDate,
                x.CenitCycleExecutionId
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("net-positions")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetNetPositionsAsync(
        [FromQuery] long? cenitCycleExecutionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var latestExecutionId = cenitCycleExecutionId
            ?? await _dbContext.CenitCycleExecutions
                .AsNoTracking()
                .OrderByDescending(x => x.ExecutedAtUtc)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync(ct);

        if (!latestExecutionId.HasValue)
        {
            return Ok(new { items = Array.Empty<object>(), total = 0, page, pageSize, cenitCycleExecutionId = (long?)null });
        }

        var query = _dbContext.CenitNetPositions
            .AsNoTracking()
            .Include(x => x.FinancialInstitution)
            .Where(x => x.CenitNettingExecution.CenitCycleExecutionId == latestExecutionId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.NetAmount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.FinancialInstitutionId,
                FinancialInstitutionName = x.FinancialInstitution.Name,
                x.DebitAmount,
                x.CreditAmount,
                x.NetAmount,
                x.ExternalLiquidity,
                x.SimulatedLiquidity,
                x.AvailableLiquidity,
                x.LiquiditySourceType,
                x.HasInsufficientFunds
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize, cenitCycleExecutionId = latestExecutionId.Value });
    }

    [HttpGet("optimization-decisions")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetOptimizationDecisionsAsync(
        [FromQuery] long? cenitCycleExecutionId,
        [FromQuery] string? decisionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _dbContext.LiquidityOptimizationDecisions
            .AsNoTracking()
            .Include(x => x.AchTransaction)
            .AsQueryable();

        if (cenitCycleExecutionId.HasValue)
        {
            query = query.Where(x => x.CenitCycleExecutionId == cenitCycleExecutionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(decisionType))
        {
            query = query.Where(x => x.DecisionType == decisionType);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.DecidedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.CenitCycleExecutionId,
                x.AchTransactionId,
                TransactionExternalId = x.AchTransaction.TransactionExternalId,
                x.AchTransaction.Reference,
                TransactionState = x.AchTransaction.State.ToString(),
                x.DecisionType,
                x.DecisionReason,
                x.Priority,
                x.LiquidityModelUsed,
                x.FromCycleId,
                x.ToCycleId,
                x.DecidedAtUtc,
                x.ValueDate,
                x.ClearingHouseCode,
                x.SourceFileReference
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("traceability")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetTraceabilityAsync(
        [FromQuery] string? state,
        [FromQuery] string? achCycleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _dbContext.AchTransactions
            .AsNoTracking()
            .Include(x => x.AchCycle)
                .ThenInclude(x => x.ClearingHouse)
            .Include(x => x.AchBatch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(x => x.State.ToString() == state);
        }

        if (!string.IsNullOrWhiteSpace(achCycleId))
        {
            query = query.Where(x => x.AchCycleId == achCycleId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.StateChangedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TransactionExternalId,
                x.Reference,
                x.Amount,
                State = x.State.ToString(),
                x.AchCycleId,
                AchCycleName = x.AchCycle.CycleName,
                x.EffectiveEntryDate,
                ClearingHouseName = x.AchCycle.ClearingHouse.Name,
                BatchId = x.AchBatchId,
                BatchSequenceNumber = x.AchBatch.BatchSequenceNumber,
                CausalCode = string.IsNullOrWhiteSpace(x.ReturnReasonCode) ? x.ContrapartidasResponseCode : x.ReturnReasonCode,
                CausalDescription = string.Empty,
                x.OriginalTraceRef,
                x.StateChangedAtUtc,
                DecisionType = _dbContext.LiquidityOptimizationDecisions
                    .Where(d => d.AchTransactionId == x.Id)
                    .OrderByDescending(d => d.DecidedAtUtc)
                    .Select(d => d.DecisionType)
                    .FirstOrDefault(),
                SourceFileReference = _dbContext.LiquidityOptimizationDecisions
                    .Where(d => d.AchTransactionId == x.Id)
                    .OrderByDescending(d => d.DecidedAtUtc)
                    .Select(d => d.SourceFileReference)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }
}
