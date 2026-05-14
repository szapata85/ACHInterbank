using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ReturnOfReturnOrchestrator : IReturnOfReturnOrchestrator
{
    private readonly AchDbContext _context;
    private readonly IAchReturnOfReturnEligibilityService _returnOfReturnEligibilityService;
    private readonly IPaymentRailContextService? _paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver? _strategyResolver;
    private readonly IPaymentRailShadowCompareService? _shadowCompareService;
    private readonly ILogger<ReturnOfReturnOrchestrator> _logger;

    public ReturnOfReturnOrchestrator(
        AchDbContext context,
        IAchReturnOfReturnEligibilityService returnOfReturnEligibilityService,
        IPaymentRailContextService? paymentRailContextService = null,
        IPaymentRailOperationalStrategyResolver? strategyResolver = null,
        IPaymentRailShadowCompareService? shadowCompareService = null,
        ILogger<ReturnOfReturnOrchestrator>? logger = null)
    {
        _context = context;
        _returnOfReturnEligibilityService = returnOfReturnEligibilityService;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
        _shadowCompareService = shadowCompareService;
        _logger = logger ?? NullLogger<ReturnOfReturnOrchestrator>.Instance;
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
        if (sourceReturn.State is not (AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr or AchTransferStateEnum.Certified))
        {
            throw new InvalidOperationException("La devolución origen no se encuentra en estado elegible para devolución de devolución.");
        }
        if (sourceReturn.SlaDeadlineAtUtc.HasValue && DateTime.UtcNow > sourceReturn.SlaDeadlineAtUtc.Value)
        {
            throw new InvalidOperationException("El plazo operativo para devolución de devolución expiró.");
        }

        var eligibility = await _returnOfReturnEligibilityService.EvaluateAsync(
            new AchReturnOfReturnEligibilityRequest(
                sourceReturn.Id,
                reasonCode,
                DateTime.UtcNow),
            ct);

        if (!eligibility.IsEligible)
        {
            var reason = eligibility.Failures.FirstOrDefault()?.Message ?? "La devolución de devolución no es elegible.";
            throw new InvalidOperationException(reason);
        }

        var duplicateExact = await _context.ReturnOfReturnFlows
            .AnyAsync(x => x.SourceReturnTransactionId == sourceReturn.Id && x.ReturnOfReturnTransactionId == returnOfReturn.Id, ct);
        if (duplicateExact)
        {
            throw new InvalidOperationException("La devolución de devolución ya está registrada para esta combinación origen/destino.");
        }

        var returnOfReturnAlreadyRegistered = await _context.ReturnOfReturnFlows
            .AnyAsync(x => x.ReturnOfReturnTransactionId == returnOfReturn.Id, ct);
        if (returnOfReturnAlreadyRegistered)
        {
            throw new InvalidOperationException("La transacción de devolución de devolución ya fue registrada.");
        }

        if (eligibility.IsUniquePerTransaction)
        {
            var sourceAlreadyHasReturnOfReturn = await _context.ReturnOfReturnFlows
                .AnyAsync(x => x.SourceReturnTransactionId == sourceReturn.Id, ct);
            if (sourceAlreadyHasReturnOfReturn)
            {
                throw new InvalidOperationException("Ya existe una devolución de devolución para la devolución origen.");
            }
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
        CompareReturnOfReturnShadow(sourceReturn, reasonCode);
        return flow;
    }

    private async Task<int> ResolveClearingHouseIdAsync(AchTransaction sourceReturn, CancellationToken ct)
    {
        if (sourceReturn.AchCycle is not null)
        {
            return sourceReturn.AchCycle.ClearingHouseId;
        }

        var clearingHouseId = await _context.AchCycles
            .AsNoTracking()
            .Where(x => x.Id == sourceReturn.AchCycleId)
            .Select(x => x.ClearingHouseId)
            .FirstOrDefaultAsync(ct);

        return clearingHouseId > 0
            ? clearingHouseId
            : throw new InvalidOperationException($"No se encontró cámara de compensación para el ciclo {sourceReturn.AchCycleId}.");
    }

    private void CompareReturnOfReturnShadow(AchTransaction sourceReturn, string reasonCode)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        try
        {
            var cycleInfo = _context.AchCycles
                .AsNoTracking()
                .Where(x => x.Id == sourceReturn.AchCycleId)
                .Select(x => new { x.ClearingHouseId, x.ClearingHouse.Code, x.ProcessingDate })
                .FirstOrDefault();
            var context = _paymentRailContextService.ResolveContext(
                cycleInfo?.ClearingHouseId,
                cycleInfo?.Code,
                sourceReturn.AchCycleId,
                cycleInfo?.ProcessingDate.Date ?? sourceReturn.EffectiveEntryDate.Date);
            var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(
                cycleInfo?.ClearingHouseId,
                cycleInfo?.Code,
                sourceReturn.AchCycleId));
            var legacyDecisionCode = $"RETURN_OF_RETURN_REGISTERED:{reasonCode}";
            var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
                context.OperationalContext,
                PaymentRailCapabilityKind.Return,
                legacyDecisionCode));
            var shadowResult = _shadowCompareService.CompareReturnOperation(
                context,
                wrapperResult,
                legacyDecisionCode,
                legacyOperationSucceeded: true);

            _logger.LogInformation(
                "PAYMENT_RAIL_SHADOW_COMPARE_RETURN_OF_RETURN|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
                shadowResult.RailCode,
                shadowResult.LegacyDecisionCode,
                shadowResult.WrapperDecisionCode,
                shadowResult.IsEquivalent,
                shadowResult.ComparisonCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PAYMENT_RAIL_SHADOW_COMPARE_RETURN_OF_RETURN_FAILED");
        }
    }
}
