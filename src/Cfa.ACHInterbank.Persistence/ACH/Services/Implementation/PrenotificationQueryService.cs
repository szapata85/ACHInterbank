using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class PrenotificationQueryService : IPrenotificationQueryService
{
    private readonly AchDbContext _context;
    private readonly IClearingHouseValidationPolicyService _policyService;

    public PrenotificationQueryService(
        AchDbContext context,
        IBankHoliday holidayService,
        IClearingHouseValidationPolicyService? policyService = null)
    {
        _context = context;
        _policyService = policyService
            ?? new ClearingHouseValidationPolicyService(context, holidayService, new OperationalCycleWindowResolver());
    }

    public async Task<PrenotificationStatusDto?> GetByReferenceAsync(string reference, CancellationToken ct = default)
    {
        var normalized = (reference ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var transaction = await BuildBaseQuery()
            .FirstOrDefaultAsync(x => x.Reference == normalized && x.IsPrenotification, ct);
        return transaction is null ? null : await MapAsync(transaction, ct);
    }

    public async Task<PrenotificationStatusDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var transaction = await BuildBaseQuery()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPrenotification, ct);
        return transaction is null ? null : await MapAsync(transaction, ct);
    }

    private IQueryable<AchTransaction> BuildBaseQuery()
        => _context.AchTransactions
            .AsNoTracking()
            .Include(x => x.SourceInstitution)
            .Include(x => x.AchCycle)
                .ThenInclude(x => x.ClearingHouse)
            .Include(x => x.StateEvents);

    private async Task<PrenotificationStatusDto> MapAsync(AchTransaction transaction, CancellationToken ct)
    {
        var direction = NachaTransactionCodeTaxonomy.ResolvePrenotificationDirection(transaction.TransactionCode);
        var nature = direction.HasValue ? TransactionPrerequisitePolicyService.ResolveNature(direction.Value) : null;
        ClearingHouseTransactionRule? rule = null;
        PrenotificationValidationAttempt? attempt = await _context.PrenotificationValidationAttempts
            .AsNoTracking()
            .Include(x => x.ThirdPartyAccountValidation)
                .ThenInclude(x => x.PolicyRule)
            .FirstOrDefaultAsync(x => x.AchTransactionId == transaction.Id, ct);

        if (attempt?.ThirdPartyAccountValidation.PolicyRule is not null)
        {
            rule = attempt.ThirdPartyAccountValidation.PolicyRule;
            nature ??= attempt.ThirdPartyAccountValidation.TransactionNature;
        }
        else if (nature.HasValue && transaction.AchCycle?.ClearingHouseId > 0)
        {
            rule = (await _policyService.ResolveAsync(
                transaction.AchCycle.ClearingHouseId,
                nature.Value,
                transaction.EffectiveEntryDate,
                ct)).Rule;
        }

        // Maturity follows the prenotification effective/processing date. SentAtUtc is transport evidence
        // and must not turn an evening Colombian cycle into the next business date through UTC conversion.
        var baseDate = attempt?.ThirdPartyAccountValidation.EffectiveDate.Date ?? transaction.EffectiveEntryDate.Date;
        var maturityDate = rule?.PrenotificationLeadBusinessDays is > 0
            ? _policyService.AddBusinessDays(baseDate, rule.PrenotificationLeadBusinessDays.Value)
            : baseDate;

        var approvedAt = attempt?.ResolvedAtUtc
            ?? transaction.StateEvents
                .Where(x => x.ToState is AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => (DateTimeOffset?)x.CreatedAt)
                .FirstOrDefault();

        var isApproved = attempt?.Resolution is PrenotificationValidationResolution.Accepted or PrenotificationValidationResolution.TacitlyAccepted
                         || transaction.State is AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified;
        var policyResolved = rule is not null && nature.HasValue;
        var isMatured = policyResolved && isApproved && DateTime.UtcNow.Date >= maturityDate.Date;
        var canBeUsedForDebit = nature == TransactionNature.Debit && isMatured;

        return new PrenotificationStatusDto
        {
            Id = transaction.Id,
            Reference = transaction.Reference,
            ClearingHouse = transaction.AchCycle.ClearingHouse?.Name ?? string.Empty,
            SourceFinancialInstitution = transaction.SourceInstitution?.Name ?? string.Empty,
            SourceIsDefault = transaction.SourceInstitution?.IsDefaultSource ?? false,
            TransactionId = transaction.Id,
            NachaCode = transaction.TransactionCode,
            Status = transaction.State.ToString(),
            StatusDescription = ToSpanishStatus(transaction.State),
            EffectiveDate = transaction.EffectiveEntryDate.Date,
            ApprovedAt = approvedAt,
            MaturityDate = maturityDate,
            IsMatured = isMatured,
            CanBeUsedForDebit = canBeUsedForDebit,
            Message = BuildMessage(transaction.State, policyResolved, isMatured, rule?.PrenotificationLeadBusinessDays)
        };
    }

    private static string ToSpanishStatus(AchTransferStateEnum state)
        => state switch
        {
            AchTransferStateEnum.Pending => "Pendiente",
            AchTransferStateEnum.AppliedTacitly => "Aprobada",
            AchTransferStateEnum.Certified => "Certificada",
            AchTransferStateEnum.ReturnedByOperator => "Rechazada por operador",
            AchTransferStateEnum.ReturnedByEpr => "Rechazada por EPR",
            _ => "Estado no clasificado"
        };

    private static string BuildMessage(
        AchTransferStateEnum state,
        bool policyResolved,
        bool isMatured,
        int? leadBusinessDays)
    {
        if (!policyResolved)
        {
            return "La prenotificación requiere revisión técnica porque no se resolvió una política efectiva de cámara/naturaleza.";
        }

        return state switch
        {
            AchTransferStateEnum.Pending => "La prenotificación está pendiente de resolución automática.",
            AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified when !isMatured && leadBusinessDays is > 0
                => $"La prenotificación fue aceptada, pero aún no cumple el lead efectivo configurado de {leadBusinessDays.Value} días hábiles.",
            AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified => "La prenotificación fue aceptada y cumple la política efectiva aplicable.",
            AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr => "La prenotificación fue rechazada automáticamente y requiere un nuevo intento cuando corresponda.",
            _ => "La prenotificación requiere revisión operativa."
        };
    }
}
