using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionPolicyService : ITransactionPolicyService
{
    private readonly AchDbContext _context;
    private readonly IRoutingStrategyService _routingStrategyService;
    private readonly TransactionPolicyOptions _options;

    public TransactionPolicyService(
        AchDbContext context,
        IRoutingStrategyService routingStrategyService,
        IOptions<TransactionPolicyOptions> options)
    {
        _context = context;
        _routingStrategyService = routingStrategyService;
        _options = options.Value ?? new TransactionPolicyOptions();
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

        var now = DateTime.Now;
        var cycleId = await _routingStrategyService.ResolveClearingHouseForTransactionAsync(request.DestinationInstitutionId, now, ct);
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo ACH resuelto para la transacción.");

        var window = BuildCycleWindow(cycle.ProcessingDate, cycle.StartTime, cycle.EndTime);
        var isWithinProcessingWindow = now >= window.Start && now <= window.End;
        if (!isWithinProcessingWindow)
        {
            return Reject(
                $"La transacción está fuera de la ventana operativa del ciclo {cycle.CycleName} ({window.Start:yyyy-MM-dd HH:mm} - {window.End:yyyy-MM-dd HH:mm}).",
                cycle,
                window,
                idempotencyKey: BuildIdempotencyKey(request, cycle.Id));
        }

        var rule = ResolveRule(cycle.ClearingHouseId, cycle.CycleName, request.Type, request.AccountType, request.IsPrenotification);
        if (rule.AllowedAccountTypes.Count > 0 && !rule.AllowedAccountTypes.Contains(request.AccountType))
        {
            return Reject($"El tipo de producto {request.AccountType} no está habilitado para {request.Type} en {cycle.CycleName}.", cycle, window, BuildIdempotencyKey(request, cycle.Id));
        }

        if (rule.MaxAmountPerTransaction.HasValue && request.Amount > rule.MaxAmountPerTransaction.Value)
        {
            return Reject($"El monto excede el límite por transacción de {rule.MaxAmountPerTransaction.Value:N2}.", cycle, window, BuildIdempotencyKey(request, cycle.Id), rule);
        }

        var cycleTransactions = await _context.AchTransactions
            .AsNoTracking()
            .Where(t => t.AchCycleId == cycle.Id && t.Type == request.Type)
            .ToListAsync(ct);

        var existingCount = cycleTransactions.Count;
        var existingAmount = cycleTransactions.Sum(t => t.Amount);

        if (rule.MaxTransactionsPerCycle.HasValue && existingCount >= rule.MaxTransactionsPerCycle.Value)
        {
            return Reject($"El ciclo alcanzó el máximo de {rule.MaxTransactionsPerCycle.Value} transacciones para {request.Type}.", cycle, window, BuildIdempotencyKey(request, cycle.Id), rule, existingCount, existingAmount);
        }

        if (rule.MaxAmountPerCycle.HasValue && existingAmount + request.Amount > rule.MaxAmountPerCycle.Value)
        {
            return Reject($"El ciclo no tiene cupo suficiente. Disponible restante: {Math.Max(0, rule.MaxAmountPerCycle.Value - existingAmount):N2}.", cycle, window, BuildIdempotencyKey(request, cycle.Id), rule, existingCount, existingAmount);
        }

        var idempotencyKey = BuildIdempotencyKey(request, cycle.Id);
        var wouldDuplicate = await _context.AchTransactions
            .AsNoTracking()
            .AnyAsync(t => t.AchCycleId == cycle.Id
                && t.Reference == request.Reference.Trim()
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
            rule.MaxAmountPerTransaction,
            rule.MaxAmountPerCycle.HasValue ? Math.Max(0, rule.MaxAmountPerCycle.Value - existingAmount) : null,
            rule.MaxTransactionsPerCycle.HasValue ? Math.Max(0, rule.MaxTransactionsPerCycle.Value - existingCount) : null,
            idempotencyKey,
            wouldDuplicate);
    }

    private TransactionLimitRule ResolveRule(int clearingHouseId, string cycleName, TransactionTypeEnum type, AccountTypeEnum accountType, bool isPrenotification)
    {
        return _options.Limits
            .Where(rule => !rule.ClearingHouseId.HasValue || rule.ClearingHouseId == clearingHouseId)
            .Where(rule => string.IsNullOrWhiteSpace(rule.CycleName) || string.Equals(rule.CycleName, cycleName, StringComparison.OrdinalIgnoreCase))
            .Where(rule => !rule.TransactionType.HasValue || rule.TransactionType == type)
            .Where(rule => !rule.IsPrenotification.HasValue || rule.IsPrenotification == isPrenotification)
            .Where(rule => rule.AllowedAccountTypes.Count == 0 || rule.AllowedAccountTypes.Contains(accountType))
            .OrderByDescending(rule => rule.ClearingHouseId.HasValue)
            .ThenByDescending(rule => !string.IsNullOrWhiteSpace(rule.CycleName))
            .ThenByDescending(rule => rule.TransactionType.HasValue)
            .ThenByDescending(rule => rule.IsPrenotification.HasValue)
            .FirstOrDefault()
            ?? _options.Defaults;
    }

    private static TransactionPolicyPreview Reject(
        string message,
        AchCycle? cycle = null,
        (DateTime Start, DateTime End)? window = null,
        string? idempotencyKey = null,
        TransactionLimitRule? rule = null,
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
            rule?.MaxAmountPerTransaction,
            rule?.MaxAmountPerCycle.HasValue == true && existingAmount.HasValue ? Math.Max(0, rule.MaxAmountPerCycle.Value - existingAmount.Value) : null,
            rule?.MaxTransactionsPerCycle.HasValue == true && existingCount.HasValue ? Math.Max(0, rule.MaxTransactionsPerCycle.Value - existingCount.Value) : null,
            idempotencyKey,
            false);
    }

    private static (DateTime Start, DateTime End) BuildCycleWindow(DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            return (processingDate.Date + startTime, processingDate.Date + endTime);
        }

        return (processingDate.Date.AddDays(-1) + startTime, processingDate.Date + endTime);
    }

    private static string BuildIdempotencyKey(TransactionPolicyPreviewRequest request, string cycleId)
    {
        return string.Join(':',
            cycleId.Trim(),
            request.Type,
            request.SourceAccountNumber.Trim(),
            request.DestinationAccountNumber.Trim(),
            request.Amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            request.Reference.Trim());
    }
}

