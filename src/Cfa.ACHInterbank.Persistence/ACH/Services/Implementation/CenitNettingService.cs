using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CenitNettingService : ICenitNettingService
{
    private readonly AchDbContext _context;
    private readonly IPaymentRailContextService? _paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver? _strategyResolver;
    private readonly IPaymentRailShadowCompareService? _shadowCompareService;
    private readonly ILogger<CenitNettingService> _logger;

    public CenitNettingService(
        AchDbContext context,
        IPaymentRailContextService? paymentRailContextService = null,
        IPaymentRailOperationalStrategyResolver? strategyResolver = null,
        IPaymentRailShadowCompareService? shadowCompareService = null,
        ILogger<CenitNettingService>? logger = null)
    {
        _context = context;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
        _shadowCompareService = shadowCompareService;
        _logger = logger ?? NullLogger<CenitNettingService>.Instance;
    }

    public async Task<CenitNettingExecution> CalculateAsync(CenitCycleExecution execution, CancellationToken ct)
    {
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .FirstAsync(x => x.Id == execution.AchCycleId, ct);
        var sourceFileReference = await _context.AchFileExports
            .AsNoTracking()
            .Where(x => x.AchCycleId == execution.AchCycleId)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .Select(x => x.FileName)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

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
                ExternalLiquidity = null,
                SimulatedLiquidity = g.Sum(x => x.Credit),
                LiquiditySourceType = "Simulated",
                AvailableLiquidity = g.Sum(x => x.Credit),
                HasInsufficientFunds = g.Sum(x => x.Credit) - g.Sum(x => x.Debit) < 0
            })
            .ToList();

        var details = txs.Select(tx => new CenitNettingDetail
        {
            AchTransactionId = tx.Id,
            SourceInstitutionId = tx.SourceInstitutionId,
            DestinationInstitutionId = tx.DestinationInstitutionId,
            AchBatchId = tx.AchBatchId,
            ValueDate = tx.EffectiveEntryDate.Date,
            ClearingHouseId = cycle.ClearingHouseId,
            ClearingHouseCode = cycle.ClearingHouse?.Code ?? string.Empty,
            SourceFileReference = sourceFileReference,
            Amount = tx.Amount,
            IncludedInSettlement = true,
            DecisionReason = "IncludedInMultilateralNetting"
        }).ToList();

        netting.NetPositions = grouped;
        netting.Details = details;

        _context.CenitNettingExecutions.Add(netting);
        await _context.SaveChangesAsync(ct);
        CompareNettingShadow(cycle, execution, details.Count, netting.TotalDebit, netting.TotalCredit);
        return netting;
    }

    private void CompareNettingShadow(AchCycle cycle, CenitCycleExecution execution, int legacyDetailCount, decimal legacyTotalDebit, decimal legacyTotalCredit)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        try
        {
            var context = _paymentRailContextService.ResolveContext(
                cycle.ClearingHouseId,
                cycle.ClearingHouse?.Code,
                execution.AchCycleId,
                cycle.ProcessingDate.Date);
            var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(
                cycle.ClearingHouseId,
                cycle.ClearingHouse?.Code,
                execution.AchCycleId));
            const string legacyDecisionCode = "CENIT_NETTING_CALCULATED";
            var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
                context.OperationalContext,
                PaymentRailCapabilityKind.Netting,
                legacyDecisionCode));
            var shadowResult = _shadowCompareService.CompareNettingOperation(
                context,
                wrapperResult,
                legacyDecisionCode,
                legacyDetailCount,
                legacyTotalDebit,
                legacyTotalCredit);

            _logger.LogInformation(
                "PAYMENT_RAIL_SHADOW_COMPARE_NETTING|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
                shadowResult.RailCode,
                shadowResult.LegacyDecisionCode,
                shadowResult.WrapperDecisionCode,
                shadowResult.IsEquivalent,
                shadowResult.ComparisonCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PAYMENT_RAIL_SHADOW_COMPARE_NETTING_FAILED");
        }
    }
}
