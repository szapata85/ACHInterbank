using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionPrerequisitePolicyService : ITransactionPrerequisitePolicyService
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;

    public TransactionPrerequisitePolicyService(AchDbContext context, IBankHoliday holidayService)
    {
        _context = context;
        _holidayService = holidayService;
    }

    public async Task<TransactionPrerequisitePreviewResponse> PreviewAsync(TransactionPrerequisitePreviewRequest request, CancellationToken ct)
    {
        var nature = ResolveNature(request.TransactionType);
        if (nature is null)
        {
            return new(false, false, PrenotificationRequirementMode.NotApplicable, false, ValidationRequirementMode.NotApplicable, null, null, "NOT_APPLICABLE", "El tipo de transacción no requiere política de prenotificación para export NACHA.");
        }

        var rule = await ResolveRuleAsync(request.ClearingHouseId, nature.Value, request.TransactionType, request.EffectiveEntryDate, request.AppliesToNachaExport, ct);
        if (rule is null)
        {
            return new(false, false, PrenotificationRequirementMode.NotApplicable, false, ValidationRequirementMode.NotApplicable, null, null, "RULE_NOT_CONFIGURED", "No existe regla vigente para cámara, naturaleza y tipo de transacción.");
        }

        var decision = rule.PrenotificationMode == PrenotificationRequirementMode.Mandatory
            ? "PRENOTIFICATION_REQUIRED"
            : rule.PrenotificationMode == PrenotificationRequirementMode.Optional
                ? "PRENOTIFICATION_OPTIONAL"
                : "PRENOTIFICATION_NOT_APPLICABLE";

        return new(
            true,
            rule.PrenotificationMode == PrenotificationRequirementMode.Mandatory,
            rule.PrenotificationMode,
            rule.RequiresReceiverIdentificationValidation,
            rule.ReceiverIdentificationValidationMode,
            rule.NormativeSource,
            rule.NormativeReference,
            decision,
            BuildDecisionMessage(rule));
    }

    public async Task<TransactionPrerequisiteValidationResult> ValidateForNachaExportAsync(AchTransaction transaction, DateTime? prenotificationDate, CancellationToken ct)
    {
        if (transaction.IsPrenotification)
        {
            return new(true, "OK", "La transacción es prenotificación.", null);
        }

        var nature = ResolveNature(transaction.Type);
        if (nature is null)
        {
            return new(true, "OK", "El tipo de transacción no requiere evaluación de prenotificación.", null);
        }

        var clearingHouseId = transaction.AchCycle?.ClearingHouseId;
        if (clearingHouseId is null or <= 0)
        {
            clearingHouseId = await _context.AchTransactions
                .AsNoTracking()
                .Where(x => x.Id == transaction.Id)
                .Select(x => (int?)x.AchCycle.ClearingHouseId)
                .FirstOrDefaultAsync(ct);
        }

        if (clearingHouseId is null or <= 0 && !string.IsNullOrWhiteSpace(transaction.AchCycleId))
        {
            clearingHouseId = await _context.AchCycles
                .AsNoTracking()
                .Where(x => x.Id == transaction.AchCycleId)
                .Select(x => (int?)x.ClearingHouseId)
                .FirstOrDefaultAsync(ct);
        }
        if (clearingHouseId is null or <= 0)
        {
            return new(false, "NACHA_EXPORT_RULE_NOT_CONFIGURED", $"La transacción {transaction.Id} no tiene cámara de compensación resuelta para evaluar reglas de exportación.", null);
        }

        var rule = await ResolveRuleAsync(clearingHouseId.Value, nature.Value, transaction.Type, transaction.EffectiveEntryDate, true, ct);
        if (rule is null)
        {
            return new(false, "NACHA_EXPORT_RULE_NOT_CONFIGURED", $"No existe regla vigente de exportación NACHA para la transacción {transaction.Id}, cámara {clearingHouseId}, naturaleza {nature.Value}.", null);
        }

        if (rule.PrenotificationMode != PrenotificationRequirementMode.Mandatory)
        {
            return new(true, "OK", "La regla vigente no bloquea por prenotificación.", rule);
        }

        if (prenotificationDate is null)
        {
            return new(false, "NACHA_EXPORT_PREREQUISITE_FAILED", $"La transacción {transaction.Id} no tiene prenotificación previa.", rule);
        }

        if (rule.PrenotificationLeadBusinessDays.HasValue)
        {
            var leadDays = rule.PrenotificationLeadBusinessDays.Value;
            var minDate = AddBusinessDays(prenotificationDate.Value.Date, leadDays);
            if (transaction.EffectiveEntryDate.Date < minDate)
            {
                return new(
                    false,
                    "NACHA_EXPORT_PREREQUISITE_FAILED",
                    $"La transacción {transaction.Id} no cumple los {leadDays} días hábiles requeridos desde la prenotificación.",
                    rule);
            }
        }

        return new(true, "OK", "La transacción cumple prerequisitos de prenotificación.", rule);
    }

    private async Task<ClearingHouseTransactionRule?> ResolveRuleAsync(
        int clearingHouseId,
        TransactionNature nature,
        TransactionTypeEnum type,
        DateTime effectiveDate,
        bool appliesToNachaExport,
        CancellationToken ct)
    {
        var candidates = await _context.ClearingHouseTransactionRules
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.TransactionNature == nature
                        && x.TransactionType == type
                        && x.IsActive
                        && x.AppliesToNachaExport == appliesToNachaExport
                        && x.AppliesToMonetaryTransactions
                        && x.EffectiveFrom.Date <= effectiveDate.Date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= effectiveDate.Date))
            .OrderByDescending(x => x.EffectiveFrom)
            .Take(2)
            .ToListAsync(ct);

        if (candidates.Count > 1)
        {
            throw new InvalidOperationException(
                $"Existe más de una política vigente para la cámara {clearingHouseId}, tipo {type} y fecha {effectiveDate:yyyy-MM-dd}.");
        }

        return candidates.Count == 0 ? null : candidates[0];
    }

    public static TransactionNature? ResolveNature(TransactionTypeEnum type)
        => type switch
        {
            TransactionTypeEnum.Credit => TransactionNature.Credit,
            TransactionTypeEnum.Debit => TransactionNature.Debit,
            _ => null
        };

    private static string BuildDecisionMessage(ClearingHouseTransactionRule rule)
        => rule.PrenotificationMode switch
        {
            PrenotificationRequirementMode.Mandatory => "La regla vigente exige prenotificación previa para exportación NACHA-M.",
            PrenotificationRequirementMode.Optional => "La regla vigente permite prenotificación opcional y no bloquea la exportación por ausencia de prenotificación.",
            _ => "La regla vigente indica que la prenotificación no aplica."
        };

    private DateTime AddBusinessDays(DateTime start, int days)
    {
        var date = start;
        var remaining = days;
        var currentYear = date.Year;
        var holidays = _holidayService.GetHolidays(currentYear)
            .Select(h => h.Date)
            .ToHashSet();

        while (remaining > 0)
        {
            date = date.AddDays(1);

            if (date.Year != currentYear)
            {
                currentYear = date.Year;
                holidays = _holidayService.GetHolidays(currentYear)
                    .Select(h => h.Date)
                    .ToHashSet();
            }

            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isHoliday = holidays.Contains(DateOnly.FromDateTime(date));
            if (!isWeekend && !isHoliday)
            {
                remaining--;
            }
        }

        return date;
    }
}
