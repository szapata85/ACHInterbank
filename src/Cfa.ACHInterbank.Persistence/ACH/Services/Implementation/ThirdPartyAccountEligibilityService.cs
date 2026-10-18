using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ThirdPartyAccountEligibilityService : IThirdPartyAccountEligibilityService
{
    private readonly AchDbContext _context;
    private readonly IClearingHouseValidationPolicyService _policyService;

    public ThirdPartyAccountEligibilityService(
        AchDbContext context,
        IClearingHouseValidationPolicyService policyService)
    {
        _context = context;
        _policyService = policyService;
    }

    public async Task<ThirdPartyAccountEligibilityResult> EvaluateAsync(
        ThirdPartyAccountEligibilityRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var account = await _context.ThirdPartyAccounts
            .AsNoTracking()
            .Include(x => x.ThirdParty)
            .FirstOrDefaultAsync(x => x.Id == request.ThirdPartyAccountId, ct);
        if (account is null
            || account.Status != ThirdPartyAdministrativeStatus.Active
            || account.ThirdParty is null
            || account.ThirdParty.Status != ThirdPartyAdministrativeStatus.Active)
        {
            return Result(ThirdPartyAccountEligibilityDecision.Blocked, "MASTER_BLOCKED", "El tercero o la cuenta canónica no existe o no está administrativamente activo.");
        }

        var policy = await _policyService.ResolveAsync(
            request.ClearingHouseId,
            request.TransactionNature,
            request.EffectiveDate,
            ct);
        if (policy.Rule is null)
        {
            return Result(ThirdPartyAccountEligibilityDecision.Unsupported, "POLICY_NOT_CONFIGURED", "No existe política efectiva para el contexto solicitado.");
        }

        if (!PolicyMatchesRequest(policy.Rule, request))
        {
            return Result(ThirdPartyAccountEligibilityDecision.TechnicalReview, "POLICY_CONTEXT_MISMATCH", "La política resuelta no corresponde íntegramente al contexto solicitado; la elegibilidad se bloquea para revisión técnica.", policy.Rule);
        }

        var serviceType = ThirdPartyAccountValidation.NormalizeServiceType(request.ServiceType);
        var validation = await _context.ThirdPartyAccountValidations
            .AsNoTracking()
            .Include(x => x.Attempts)
            .SingleOrDefaultAsync(x => x.ThirdPartyAccountId == request.ThirdPartyAccountId
                                      && x.ClearingHouseId == request.ClearingHouseId
                                      && x.TransactionNature == request.TransactionNature
                                      && x.ServiceType == serviceType
                                      && x.ValidationType == ThirdPartyAccountValidationType.Prenotification,
                ct);

        if (validation is null)
        {
            return policy.Rule.PrenotificationMode == PrenotificationRequirementMode.Mandatory
                ? Result(ThirdPartyAccountEligibilityDecision.RequiresPrenotification, "PRENOTIFICATION_REQUIRED", "La política efectiva exige prenotificación.", policy.Rule)
                : Result(ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification, "PRENOTIFICATION_OPTIONAL", "La política efectiva permite operar sin prenotificación.", policy.Rule);
        }

        var latest = validation.Attempts
            .OrderByDescending(x => x.AttemptNumber)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
        if (validation.PolicyRuleId != policy.Rule.Id)
        {
            // Functional policy changes are versioned as new rule rows. A validation produced under a
            // different functional version cannot silently authorize the new effective policy.
            return Result(ThirdPartyAccountEligibilityDecision.RequiresRevalidation, "POLICY_VERSION_CHANGED", "La validación existente corresponde a una versión funcional de política distinta y requiere revalidación.", policy.Rule, validation, latest?.Id);
        }

        if (validation.EffectiveDate.Date > request.EffectiveDate.Date)
        {
            return Result(ThirdPartyAccountEligibilityDecision.PrenotificationPending, "VALIDATION_NOT_EFFECTIVE_YET", "La validación contextual corresponde a una fecha posterior a la solicitada y aún no puede autorizarla.", policy.Rule, validation, latest?.Id);
        }

        var expiresAtUtc = validation.ExpiresAtUtc;
        if (!expiresAtUtc.HasValue
            && validation.LastMonetaryUseAtUtc.HasValue
            && policy.Rule.ValidationRetentionMonthsAfterLastMonetaryUse is > 0)
        {
            // La expiración se deriva sólo cuando la política efectiva aporta una retención aprobada.
            // CENIT débito usa esta autoridad; no se extrapola a cámaras cuya regla no la configure.
            expiresAtUtc = validation.LastMonetaryUseAtUtc.Value
                .AddMonths(policy.Rule.ValidationRetentionMonthsAfterLastMonetaryUse.Value);
        }

        if (expiresAtUtc.HasValue
            && expiresAtUtc.Value <= new DateTimeOffset(DateTime.SpecifyKind(request.EffectiveDate.Date, DateTimeKind.Utc)))
        {
            return Result(ThirdPartyAccountEligibilityDecision.ValidationExpired, "VALIDATION_EXPIRED", "La validación contextual expiró y requiere revalidación.", policy.Rule, validation, latest?.Id);
        }

        switch (validation.Status)
        {
            case ThirdPartyAccountValidationStatus.PendingSend:
            case ThirdPartyAccountValidationStatus.PendingResponse:
                return Result(ThirdPartyAccountEligibilityDecision.PrenotificationPending, "PRENOTIFICATION_PENDING", "Existe una prenotificación pendiente de resolución.", policy.Rule, validation, latest?.Id);
            case ThirdPartyAccountValidationStatus.Rejected:
                return Result(ThirdPartyAccountEligibilityDecision.Rejected, "VALIDATION_REJECTED", "La última validación fue rechazada.", policy.Rule, validation, latest?.Id);
            case ThirdPartyAccountValidationStatus.RequiresCorrection:
                return Result(ThirdPartyAccountEligibilityDecision.RequiresRevalidation, "REVALIDATION_REQUIRED", "La validación requiere corrección y un nuevo intento.", policy.Rule, validation, latest?.Id);
            case ThirdPartyAccountValidationStatus.TechnicalReview:
            case ThirdPartyAccountValidationStatus.Suspended:
                return Result(ThirdPartyAccountEligibilityDecision.TechnicalReview, "TECHNICAL_REVIEW_REQUIRED", "El contexto requiere revisión técnica; no existe una decisión interbancaria automática utilizable.", policy.Rule, validation, latest?.Id);
            case ThirdPartyAccountValidationStatus.Expired:
                return Result(ThirdPartyAccountEligibilityDecision.ValidationExpired, "VALIDATION_EXPIRED", "La validación contextual expiró.", policy.Rule, validation, latest?.Id);
            case ThirdPartyAccountValidationStatus.Accepted:
            case ThirdPartyAccountValidationStatus.TacitlyAccepted:
                if (!HasConsistentPositiveEvidence(validation, latest))
                {
                    return Result(ThirdPartyAccountEligibilityDecision.TechnicalReview, "VALIDATION_EVIDENCE_INCONSISTENT", "El estado positivo no cuenta con evidencia append-only consistente para autorizar la operación.", policy.Rule, validation, latest?.Id);
                }
                break;
            default:
                return policy.Rule.PrenotificationMode == PrenotificationRequirementMode.Mandatory
                    ? Result(ThirdPartyAccountEligibilityDecision.RequiresPrenotification, "PRENOTIFICATION_REQUIRED", "No existe una validación utilizable.", policy.Rule, validation, latest?.Id)
                    : Result(ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification, "PRENOTIFICATION_OPTIONAL", "La prenotificación es opcional y no existe una validación utilizable.", policy.Rule, validation, latest?.Id);
        }

        if (latest is not null && policy.Rule.PrenotificationLeadBusinessDays is > 0)
        {
            // Business-day lead is anchored to the prenotification effective/processing date,
            // never to the UTC calendar date of the transport timestamp.
            var minimumEffectiveDate = _policyService.AddBusinessDays(validation.EffectiveDate.Date, policy.Rule.PrenotificationLeadBusinessDays.Value);
            if (request.EffectiveDate.Date < minimumEffectiveDate)
            {
                return Result(ThirdPartyAccountEligibilityDecision.PrenotificationPending, "PRENOTIFICATION_LEAD_NOT_MET", $"La prenotificación existe pero aún no cumple el lead efectivo de {policy.Rule.PrenotificationLeadBusinessDays.Value} días hábiles.", policy.Rule, validation, latest.Id);
            }
        }

        return Result(ThirdPartyAccountEligibilityDecision.Allowed, "ELIGIBLE", "La cuenta es elegible en el contexto solicitado según política efectiva e historial de validación.", policy.Rule, validation, latest?.Id);
    }

    private static bool PolicyMatchesRequest(
        ClearingHouseTransactionRule rule,
        ThirdPartyAccountEligibilityRequest request)
    {
        var effectiveDate = request.EffectiveDate.Date;
        return rule.IsActive
               && rule.ClearingHouseId == request.ClearingHouseId
               && rule.TransactionNature == request.TransactionNature
               && rule.EffectiveFrom.Date <= effectiveDate
               && (!rule.EffectiveTo.HasValue || rule.EffectiveTo.Value.Date >= effectiveDate);
    }

    private static bool HasConsistentPositiveEvidence(
        ThirdPartyAccountValidation validation,
        PrenotificationValidationAttempt? latest)
    {
        if (latest is null)
        {
            return false;
        }

        if (validation.CurrentAttemptId.HasValue && validation.CurrentAttemptId.Value != latest.Id)
        {
            return false;
        }

        return validation.Status switch
        {
            ThirdPartyAccountValidationStatus.Accepted => latest.Resolution == PrenotificationValidationResolution.Accepted,
            ThirdPartyAccountValidationStatus.TacitlyAccepted => latest.Resolution == PrenotificationValidationResolution.TacitlyAccepted,
            _ => false
        };
    }

    private static ThirdPartyAccountEligibilityResult Result(
        ThirdPartyAccountEligibilityDecision decision,
        string code,
        string message,
        ClearingHouseTransactionRule? rule = null,
        ThirdPartyAccountValidation? validation = null,
        long? attemptId = null)
        => new(decision, code, message, rule, validation?.Status, validation?.Id, attemptId);
}
