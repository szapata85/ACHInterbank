using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionPolicyService : ITransactionPolicyService
{
    private readonly AchDbContext _context;
    private readonly IRoutingStrategyService _routingStrategyService;
    private readonly ICfaOperationalTransactionRestrictionResolver _operationalRestrictionResolver;
    private readonly TimeProvider _timeProvider;
    private readonly IOperationalCycleWindowResolver _windowResolver;
    private readonly ICycleTransactionPolicy _cycleTransactionPolicy;

    public TransactionPolicyService(
        AchDbContext context,
        IRoutingStrategyService routingStrategyService,
        ICfaOperationalTransactionRestrictionResolver operationalRestrictionResolver,
        TimeProvider? timeProvider = null,
        IOperationalCycleWindowResolver? windowResolver = null,
        ICycleTransactionPolicy? cycleTransactionPolicy = null)
    {
        _context = context;
        _routingStrategyService = routingStrategyService;
        _operationalRestrictionResolver = operationalRestrictionResolver;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _windowResolver = windowResolver ?? new OperationalCycleWindowResolver();
        _cycleTransactionPolicy = cycleTransactionPolicy ?? new CycleTransactionPolicy(
            new ClearingHouseCyclePolicyResolver(context, new CycleNumberResolver()),
            new ClearingHouseToPaymentRailMapper(),
            new CycleNumberResolver());
    }

    public async Task<TransactionPolicyPreview> PreviewAsync(TransactionPolicyPreviewRequest request, CancellationToken ct = default)
    {
        if (request.DestinationInstitutionId <= 0)
        {
            return Reject("Debe seleccionar una institución destino.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceAccountNumber))
        {
            return Reject("La cuenta origen es obligatoria para validar políticas ACH.");
        }

        if (string.IsNullOrWhiteSpace(request.DestinationAccountNumber))
        {
            return Reject("La cuenta destino es obligatoria para validar políticas ACH.");
        }

        // 1. Resolve the operational cycle first. The resolved cycle carries the versioned
        // ClearingHouseCycleConfigId that isolates both regulatory and CFA operational rules.
        var nowInstant = _timeProvider.GetUtcNow();
        var cycleId = await _routingStrategyService.ResolveClearingHouseForTransactionAsync(
            request.DestinationInstitutionId,
            nowInstant.UtcDateTime,
            ct);
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
                .ThenInclude(clearingHouse => clearingHouse!.ClearingHouseConfig)
            .FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo ACH resuelto para la transacción.");

        var idempotencyKey = BuildIdempotencyKey(request, cycle.Id);

        // 2-3. Regulation is the absolute first business-policy gate. CFA operational
        // restrictions can only tighten an ALLOW; they can never override a regulatory DENY.
        var regulatoryDecision = await _cycleTransactionPolicy.EvaluateAsync(new CycleTransactionPolicyRequest(
            cycle.ClearingHouseId,
            cycle.ClearingHouseCycleConfigId,
            cycle.ProcessingDate,
            cycle.ClearingHouse?.Code,
            cycle.ClearingHouse?.ClearingHouseConfig?.PaymentRailCode,
            cycle.CycleName,
            request.Type,
            request.IsPrenotification), ct);
        if (!regulatoryDecision.IsAllowed)
        {
            return Reject(
                $"{regulatoryDecision.ReasonCode}: {regulatoryDecision.Message}",
                cycle,
                idempotencyKey: idempotencyKey);
        }

        // 4. Apply persisted CFA operational restrictions using the versioned cycle-config id.
        var restriction = await _operationalRestrictionResolver.ResolveAsync(
            new CfaOperationalTransactionRestrictionRequest(
                cycle.ClearingHouseId,
                cycle.ClearingHouseCycleConfigId,
                request.Type,
                request.IsPrenotification,
                request.AccountType,
                DateTime.SpecifyKind(cycle.ProcessingDate.Date, DateTimeKind.Utc)),
            ct);

        if (restriction.IsOperationallyDenied)
        {
            return Reject(
                restriction.DenyReason
                    ?? $"El tipo de producto {request.AccountType} no está habilitado por las restricciones operacionales CFA.",
                cycle,
                idempotencyKey: idempotencyKey,
                restriction: restriction);
        }

        // 5. Existing operational controls continue after regulation + CFA restriction resolution.
        var resolvedWindow = _windowResolver.Resolve(
            cycle.ProcessingDate,
            cycle.StartTime,
            cycle.CutoffTime,
            ClearingHouseOperationalTimeZone.Resolve(cycle),
            nowInstant);
        var window = (Start: resolvedWindow.LocalStart, End: resolvedWindow.LocalEnd);
        var isWithinProcessingWindow = resolvedWindow.IsInside;
        if (!isWithinProcessingWindow)
        {
            return Reject(
                $"La transacción está fuera de la ventana de entrada del ciclo {cycle.CycleName} ({window.Start:yyyy-MM-dd HH:mm} - {window.End:yyyy-MM-dd HH:mm}).",
                cycle,
                window,
                idempotencyKey,
                restriction);
        }

        if (restriction.MaxAmountPerTransaction.HasValue && request.Amount > restriction.MaxAmountPerTransaction.Value)
        {
            return Reject(
                $"El monto excede el límite por transacción de {restriction.MaxAmountPerTransaction.Value:N2}.",
                cycle,
                window,
                idempotencyKey,
                restriction);
        }

        var cycleTransactions = await _context.AchTransactions
            .AsNoTracking()
            .Where(t => t.AchCycleId == cycle.Id && t.Type == request.Type)
            .ToListAsync(ct);

        var existingCount = cycleTransactions.Count;
        var existingAmount = cycleTransactions.Sum(t => t.Amount);

        if (restriction.MaxTransactionsPerCycle.HasValue && existingCount >= restriction.MaxTransactionsPerCycle.Value)
        {
            return Reject(
                $"El ciclo alcanzó el máximo de {restriction.MaxTransactionsPerCycle.Value} transacciones para {request.Type}.",
                cycle,
                window,
                idempotencyKey,
                restriction,
                existingCount,
                existingAmount);
        }

        if (restriction.MaxAmountPerCycle.HasValue && existingAmount + request.Amount > restriction.MaxAmountPerCycle.Value)
        {
            return Reject(
                $"El ciclo no tiene cupo suficiente. Disponible restante: {Math.Max(0, restriction.MaxAmountPerCycle.Value - existingAmount):N2}.",
                cycle,
                window,
                idempotencyKey,
                restriction,
                existingCount,
                existingAmount);
        }

        // 6. Duplicate detection and the rest of the existing preview contract remain unchanged.
        var operationalId = ResolveOperationalId(request.TransactionExternalId, request.Reference);
        var wouldDuplicate = await _context.AchTransactions
            .AsNoTracking()
            .AnyAsync(t => t.AchCycleId == cycle.Id
                && (t.TransactionExternalId == operationalId
                    || ((t.TransactionExternalId == null || t.TransactionExternalId == "") && t.Reference == operationalId))
                && t.Amount == request.Amount
                && t.SourceAccountNumber == request.SourceAccountNumber.Trim()
                && t.DestinationAccountNumber == request.DestinationAccountNumber.Trim()
                && t.Type == request.Type,
                ct);

        return new TransactionPolicyPreview(
            !wouldDuplicate,
            wouldDuplicate ? "Ya existe una transacción equivalente para el mismo ciclo." : null,
            cycle.Id,
            cycle.CycleName,
            cycle.ProcessingDate,
            cycle.ClearingHouse?.Name,
            cycle.ClearingHouseId,
            $"{window.Start:yyyy-MM-dd HH:mm} - {window.End:yyyy-MM-dd HH:mm}",
            isWithinProcessingWindow,
            restriction.MaxAmountPerTransaction,
            restriction.MaxAmountPerCycle.HasValue ? Math.Max(0, restriction.MaxAmountPerCycle.Value - existingAmount) : null,
            restriction.MaxTransactionsPerCycle.HasValue ? Math.Max(0, restriction.MaxTransactionsPerCycle.Value - existingCount) : null,
            idempotencyKey,
            wouldDuplicate);
    }

    private static TransactionPolicyPreview Reject(
        string message,
        AchCycle? cycle = null,
        (DateTime Start, DateTime End)? window = null,
        string? idempotencyKey = null,
        CfaOperationalTransactionRestrictionResolution? restriction = null,
        int? existingCount = null,
        decimal? existingAmount = null)
    {
        return new TransactionPolicyPreview(
            false,
            message,
            cycle?.Id,
            cycle?.CycleName,
            cycle?.ProcessingDate,
            cycle?.ClearingHouse?.Name,
            cycle?.ClearingHouseId,
            window.HasValue ? $"{window.Value.Start:yyyy-MM-dd HH:mm} - {window.Value.End:yyyy-MM-dd HH:mm}" : null,
            false,
            restriction?.MaxAmountPerTransaction,
            restriction?.MaxAmountPerCycle.HasValue == true && existingAmount.HasValue
                ? Math.Max(0, restriction.MaxAmountPerCycle.Value - existingAmount.Value)
                : null,
            restriction?.MaxTransactionsPerCycle.HasValue == true && existingCount.HasValue
                ? Math.Max(0, restriction.MaxTransactionsPerCycle.Value - existingCount.Value)
                : null,
            idempotencyKey,
            false);
    }

    private static string BuildIdempotencyKey(TransactionPolicyPreviewRequest request, string cycleId)
    {
        return string.Join(':',
            cycleId.Trim(),
            request.Type,
            request.SourceAccountNumber.Trim(),
            request.DestinationAccountNumber.Trim(),
            request.Amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            ResolveOperationalId(request.TransactionExternalId, request.Reference));
    }

    private static string ResolveOperationalId(string? transactionExternalId, string? reference)
    {
        if (!string.IsNullOrWhiteSpace(transactionExternalId))
        {
            return transactionExternalId.Trim();
        }

        return (reference ?? string.Empty).Trim();
    }
}
