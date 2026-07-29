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
public class ClearingHouseTransactionRuleService : IClearingHouseTransactionRuleService
{
    private readonly AchDbContext _context;

    public ClearingHouseTransactionRuleService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ClearingHouseTransactionRuleDto>> GetAsync(
        int? clearingHouseId,
        string? transactionNature,
        bool includeInactive,
        CancellationToken ct)
    {
        var query = BaseQuery();

        if (clearingHouseId.HasValue)
        {
            query = query.Where(x => x.ClearingHouseId == clearingHouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(transactionNature)
            && Enum.TryParse<TransactionNature>(transactionNature, true, out var nature))
        {
            query = query.Where(x => x.TransactionNature == nature);
        }

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var rows = await query
            .OrderBy(x => x.ClearingHouse.Name)
            .ThenBy(x => x.TransactionType)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<ClearingHouseTransactionRuleDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var rule = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        return rule is null ? null : Map(rule);
    }

    public async Task<IReadOnlyList<ClearingHouseTransactionRuleDto>> GetVersionsAsync(
        int clearingHouseId,
        TransactionTypeEnum? transactionType,
        CancellationToken ct)
    {
        await EnsureClearingHouseExistsAsync(clearingHouseId, ct);
        var query = BaseQuery().Where(x => x.ClearingHouseId == clearingHouseId);
        if (transactionType.HasValue)
        {
            EnsureSupportedType(transactionType.Value);
            query = query.Where(x => x.TransactionType == transactionType.Value);
        }

        var rows = await query
            .OrderBy(x => x.TransactionType)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<ClearingHouseTransactionRuleDto?> GetCurrentAsync(
        int clearingHouseId,
        TransactionTypeEnum transactionType,
        DateTime effectiveAt,
        CancellationToken ct)
    {
        await EnsureClearingHouseExistsAsync(clearingHouseId, ct);
        EnsureSupportedType(transactionType);

        var candidates = await BaseQuery()
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.TransactionType == transactionType
                        && x.IsActive
                        && x.EffectiveFrom.Date <= effectiveAt.Date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= effectiveAt.Date))
            .OrderByDescending(x => x.EffectiveFrom)
            .Take(2)
            .ToListAsync(ct);

        if (candidates.Count > 1)
        {
            throw new InvalidOperationException("Existe más de una política vigente para la cámara, tipo y fecha indicados.");
        }

        return candidates.Count == 0 ? null : Map(candidates[0]);
    }

    public async Task<ClearingHouseTransactionRuleDto?> GetByIdAsync(int clearingHouseId, int id, CancellationToken ct)
    {
        await EnsureClearingHouseExistsAsync(clearingHouseId, ct);
        var rule = await BaseQuery()
            .FirstOrDefaultAsync(x => x.Id == id && x.ClearingHouseId == clearingHouseId, ct);
        return rule is null ? null : Map(rule);
    }

    public async Task<ClearingHouseTransactionRuleDto> CreateAsync(
        CreateClearingHouseTransactionRuleRequest request,
        CancellationToken ct)
    {
        ValidateCompatibilityRequest(
            request.TransactionNature,
            request.TransactionType,
            request.RequiresPrenotification,
            request.PrenotificationMode,
            request.PrenotificationLeadBusinessDays,
            request.AppliesToNachaExport,
            request.AppliesToMonetaryTransactions);

        return await CreateVersionCoreAsync(
            request.ClearingHouseId,
            request.TransactionType,
            request.PrenotificationMode,
            request.PrenotificationLeadBusinessDays,
            request.EffectiveFrom,
            request.EffectiveTo,
            isActive: true,
            request.NormativeSource,
            request.NormativeReference,
            request.Notes,
            request.RequiresReceiverIdentificationValidation,
            request.ReceiverIdentificationValidationMode,
            ct);
    }

    public async Task<ClearingHouseTransactionRuleDto> CreateVersionAsync(
        int clearingHouseId,
        CreateClearingHouseTransactionPolicyVersionRequest request,
        CancellationToken ct)
    {
        var requiresValidation = request.PrenotificationMode == PrenotificationRequirementMode.Mandatory;
        return await CreateVersionCoreAsync(
            clearingHouseId,
            request.TransactionType,
            request.PrenotificationMode,
            request.PrenotificationLeadBusinessDays,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.IsActive,
            request.NormativeSource,
            request.NormativeReference,
            request.Notes,
            requiresValidation,
            requiresValidation ? ValidationRequirementMode.Mandatory : ValidationRequirementMode.Optional,
            ct);
    }

    public async Task<ClearingHouseTransactionRuleDto> UpdateAsync(
        int id,
        UpdateClearingHouseTransactionRuleRequest request,
        CancellationToken ct)
    {
        var entity = await _context.ClearingHouseTransactionRules
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("La política de cámara no existe.");

        ValidateCompatibilityRequest(
            request.TransactionNature,
            request.TransactionType,
            request.RequiresPrenotification,
            request.PrenotificationMode,
            request.PrenotificationLeadBusinessDays,
            request.AppliesToNachaExport,
            request.AppliesToMonetaryTransactions);

        if (request.ClearingHouseId != entity.ClearingHouseId || request.TransactionType != entity.TransactionType)
        {
            throw new InvalidOperationException("No se puede cambiar la cámara o el tipo de una versión existente.");
        }

        var functionalChange =
            request.PrenotificationMode != entity.PrenotificationMode
            || request.PrenotificationLeadBusinessDays != entity.PrenotificationLeadBusinessDays
            || request.EffectiveFrom.Date != entity.EffectiveFrom.Date
            || request.EffectiveTo?.Date != entity.EffectiveTo?.Date;

        if (functionalChange)
        {
            if (request.EffectiveFrom.Date <= entity.EffectiveFrom.Date)
            {
                throw new InvalidOperationException(
                    "Una decisión funcional debe crear una versión con vigencia posterior a la versión existente.");
            }

            return await CreateVersionCoreAsync(
                entity.ClearingHouseId,
                entity.TransactionType,
                request.PrenotificationMode,
                request.PrenotificationLeadBusinessDays,
                request.EffectiveFrom,
                request.EffectiveTo,
                entity.IsActive,
                request.NormativeSource,
                request.NormativeReference,
                request.Notes,
                request.RequiresReceiverIdentificationValidation,
                request.ReceiverIdentificationValidationMode,
                ct);
        }

        ValidateMetadata(request.NormativeSource, request.NormativeReference);
        entity.NormativeSource = request.NormativeSource.Trim();
        entity.NormativeReference = request.NormativeReference.Trim();
        entity.Notes = request.Notes?.Trim() ?? string.Empty;
        entity.RequiresReceiverIdentificationValidation = request.RequiresReceiverIdentificationValidation;
        entity.ReceiverIdentificationValidationMode = request.ReceiverIdentificationValidationMode;
        SynchronizeCompatibilityFields(entity);
        await _context.SaveChangesAsync(ct);

        return await GetByIdRequiredAsync(entity.Id, ct);
    }

    public async Task<ClearingHouseTransactionRuleDto> UpdateMetadataAsync(
        int clearingHouseId,
        int id,
        UpdateClearingHouseTransactionPolicyMetadataRequest request,
        CancellationToken ct)
    {
        ValidateMetadata(request.NormativeSource, request.NormativeReference);
        var entity = await GetEntityRequiredAsync(clearingHouseId, id, ct);
        entity.NormativeSource = request.NormativeSource.Trim();
        entity.NormativeReference = request.NormativeReference.Trim();
        entity.Notes = request.Notes?.Trim() ?? string.Empty;
        await _context.SaveChangesAsync(ct);
        return await GetByIdRequiredAsync(id, ct);
    }

    public async Task<ClearingHouseTransactionRuleDto> CloseVersionAsync(
        int clearingHouseId,
        int id,
        CloseClearingHouseTransactionPolicyVersionRequest request,
        CancellationToken ct)
    {
        var entity = await GetEntityRequiredAsync(clearingHouseId, id, ct);
        var closingDate = request.EffectiveTo.Date;
        if (closingDate < entity.EffectiveFrom.Date)
        {
            throw new InvalidOperationException("La fecha de cierre no puede ser anterior al inicio de vigencia.");
        }

        var nextVersionDate = await _context.ClearingHouseTransactionRules
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.TransactionType == entity.TransactionType
                        && x.Id != id
                        && x.EffectiveFrom > entity.EffectiveFrom)
            .OrderBy(x => x.EffectiveFrom)
            .Select(x => (DateTime?)x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (nextVersionDate.HasValue && closingDate >= nextVersionDate.Value.Date)
        {
            throw new InvalidOperationException("La fecha de cierre se solapa con la siguiente versión.");
        }

        entity.EffectiveTo = closingDate;
        await _context.SaveChangesAsync(ct);
        return await GetByIdRequiredAsync(id, ct);
    }

    public async Task<ClearingHouseTransactionRuleDto> ActivateVersionAsync(
        int clearingHouseId,
        int id,
        CancellationToken ct)
    {
        var entity = await GetEntityRequiredAsync(clearingHouseId, id, ct);
        await EnsureNoActiveOverlapAsync(entity, ct);
        entity.IsActive = true;
        await _context.SaveChangesAsync(ct);
        return await GetByIdRequiredAsync(id, ct);
    }

    public async Task<ClearingHouseTransactionRuleDto> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var entity = await _context.ClearingHouseTransactionRules
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("La política de cámara no existe.");

        if (isActive)
        {
            await EnsureNoActiveOverlapAsync(entity, ct);
        }

        entity.IsActive = isActive;
        await _context.SaveChangesAsync(ct);
        return await GetByIdRequiredAsync(id, ct);
    }

    private async Task<ClearingHouseTransactionRuleDto> CreateVersionCoreAsync(
        int clearingHouseId,
        TransactionTypeEnum transactionType,
        PrenotificationRequirementMode prenotificationMode,
        int? prenotificationLeadBusinessDays,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        bool isActive,
        string normativeSource,
        string normativeReference,
        string? notes,
        bool requiresReceiverIdentificationValidation,
        ValidationRequirementMode receiverIdentificationValidationMode,
        CancellationToken ct)
    {
        await EnsureClearingHouseExistsAsync(clearingHouseId, ct);
        EnsureSupportedType(transactionType);
        ValidateCanonicalDecision(
            prenotificationMode,
            prenotificationLeadBusinessDays,
            effectiveFrom,
            effectiveTo,
            normativeSource,
            normativeReference);

        var from = effectiveFrom.Date;
        var to = effectiveTo?.Date;
        var versions = await _context.ClearingHouseTransactionRules
            .Where(x => x.ClearingHouseId == clearingHouseId && x.TransactionType == transactionType)
            .OrderBy(x => x.EffectiveFrom)
            .ToListAsync(ct);

        if (versions.Any(x => x.EffectiveFrom.Date == from))
        {
            throw new InvalidOperationException("Ya existe una versión para la misma cámara, tipo y fecha inicial.");
        }

        var previous = versions.LastOrDefault(x => x.EffectiveFrom.Date < from);
        var next = versions.FirstOrDefault(x => x.EffectiveFrom.Date > from);

        if (next is not null)
        {
            var latestAllowed = next.EffectiveFrom.Date.AddDays(-1);
            if (to.HasValue && to.Value > latestAllowed)
            {
                throw new InvalidOperationException("La vigencia de la nueva versión se solapa con una versión posterior.");
            }

            to ??= latestAllowed;
        }

        if (previous is not null
            && (!previous.EffectiveTo.HasValue || previous.EffectiveTo.Value.Date >= from))
        {
            previous.EffectiveTo = from.AddDays(-1);
        }

        var nature = TransactionPrerequisitePolicyService.ResolveNature(transactionType)
            ?? throw new InvalidOperationException("El tipo de transacción no admite una política por cámara.");

        var entity = new ClearingHouseTransactionRule
        {
            ClearingHouseId = clearingHouseId,
            TransactionNature = nature,
            TransactionType = transactionType,
            PrenotificationMode = prenotificationMode,
            PrenotificationLeadBusinessDays = prenotificationLeadBusinessDays,
            RequiresReceiverIdentificationValidation = requiresReceiverIdentificationValidation,
            ReceiverIdentificationValidationMode = receiverIdentificationValidationMode,
            AppliesToNachaExport = true,
            AppliesToMonetaryTransactions = true,
            EffectiveFrom = from,
            EffectiveTo = to,
            IsActive = isActive,
            NormativeSource = normativeSource.Trim(),
            NormativeReference = normativeReference.Trim(),
            Notes = notes?.Trim() ?? string.Empty
        };
        SynchronizeCompatibilityFields(entity);

        if (isActive && versions.Any(x =>
                x.Id != previous?.Id
                && x.IsActive
                && x.EffectiveFrom.Date <= (to ?? DateTime.MaxValue.Date)
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= from)))
        {
            throw new InvalidOperationException("Existe una política activa con vigencia solapada para la cámara y el tipo.");
        }

        _context.ClearingHouseTransactionRules.Add(entity);
        await _context.SaveChangesAsync(ct);
        return await GetByIdRequiredAsync(entity.Id, ct);
    }

    private async Task EnsureNoActiveOverlapAsync(ClearingHouseTransactionRule entity, CancellationToken ct)
    {
        var from = entity.EffectiveFrom.Date;
        var to = entity.EffectiveTo?.Date ?? DateTime.MaxValue.Date;
        var overlap = await _context.ClearingHouseTransactionRules.AnyAsync(x =>
            x.Id != entity.Id
            && x.IsActive
            && x.ClearingHouseId == entity.ClearingHouseId
            && x.TransactionType == entity.TransactionType
            && x.EffectiveFrom <= to
            && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= from), ct);

        if (overlap)
        {
            throw new InvalidOperationException("Existe una política activa con vigencia solapada para la cámara y el tipo.");
        }
    }

    private static void ValidateCompatibilityRequest(
        TransactionNature nature,
        TransactionTypeEnum transactionType,
        bool requiresPrenotification,
        PrenotificationRequirementMode prenotificationMode,
        int? prenotificationLeadBusinessDays,
        bool appliesToNachaExport,
        bool appliesToMonetaryTransactions)
    {
        var expectedNature = TransactionPrerequisitePolicyService.ResolveNature(transactionType)
            ?? throw new InvalidOperationException("El tipo de transacción no admite una política por cámara.");
        if (nature != expectedNature)
        {
            throw new InvalidOperationException("La naturaleza no corresponde al tipo de transacción.");
        }

        var expectedRequirement = prenotificationMode == PrenotificationRequirementMode.Mandatory;
        if (requiresPrenotification != expectedRequirement)
        {
            throw new InvalidOperationException("RequiresPrenotification debe derivarse de PrenotificationMode.");
        }

        if (!appliesToNachaExport || !appliesToMonetaryTransactions)
        {
            throw new InvalidOperationException("La política canónica aplica siempre a exportación NACHA-M y transacciones monetarias.");
        }

        ValidateLeadDays(prenotificationMode, prenotificationLeadBusinessDays);
    }

    private static void ValidateCanonicalDecision(
        PrenotificationRequirementMode prenotificationMode,
        int? prenotificationLeadBusinessDays,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        string normativeSource,
        string normativeReference)
    {
        if (!Enum.IsDefined(prenotificationMode))
        {
            throw new InvalidOperationException("El modo de prenotificación no es válido.");
        }

        ValidateLeadDays(prenotificationMode, prenotificationLeadBusinessDays);
        if (effectiveTo.HasValue && effectiveTo.Value.Date < effectiveFrom.Date)
        {
            throw new InvalidOperationException("La vigencia hasta no puede ser menor que la vigencia desde.");
        }

        ValidateMetadata(normativeSource, normativeReference);
    }

    private static void ValidateLeadDays(
        PrenotificationRequirementMode prenotificationMode,
        int? prenotificationLeadBusinessDays)
    {
        if (prenotificationLeadBusinessDays < 0)
        {
            throw new InvalidOperationException("El plazo previo de prenotificación no puede ser negativo.");
        }

        if (prenotificationMode != PrenotificationRequirementMode.Mandatory
            && prenotificationLeadBusinessDays.HasValue)
        {
            throw new InvalidOperationException("Solo una prenotificación obligatoria puede definir plazo previo.");
        }
    }

    private static void ValidateMetadata(string normativeSource, string normativeReference)
    {
        if (string.IsNullOrWhiteSpace(normativeSource))
        {
            throw new InvalidOperationException("La fuente normativa es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(normativeReference))
        {
            throw new InvalidOperationException("La referencia normativa es obligatoria.");
        }
    }

    private static void EnsureSupportedType(TransactionTypeEnum transactionType)
    {
        if (TransactionPrerequisitePolicyService.ResolveNature(transactionType) is null)
        {
            throw new InvalidOperationException("El tipo de transacción no admite una política por cámara.");
        }
    }

    private async Task EnsureClearingHouseExistsAsync(int clearingHouseId, CancellationToken ct)
    {
        if (!await _context.ClearingHouses.AnyAsync(x => x.Id == clearingHouseId, ct))
        {
            throw new InvalidOperationException("La cámara de compensación no existe.");
        }
    }

    private async Task<ClearingHouseTransactionRule> GetEntityRequiredAsync(
        int clearingHouseId,
        int id,
        CancellationToken ct)
        => await _context.ClearingHouseTransactionRules
            .FirstOrDefaultAsync(x => x.Id == id && x.ClearingHouseId == clearingHouseId, ct)
            ?? throw new InvalidOperationException("La política no existe para la cámara indicada.");

    private async Task<ClearingHouseTransactionRuleDto> GetByIdRequiredAsync(int id, CancellationToken ct)
        => await GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar la política guardada.");

    private IQueryable<ClearingHouseTransactionRule> BaseQuery()
        => _context.ClearingHouseTransactionRules
            .AsNoTracking()
            .Include(x => x.ClearingHouse);

    private static void SynchronizeCompatibilityFields(ClearingHouseTransactionRule entity)
    {
        entity.TransactionNature = TransactionPrerequisitePolicyService.ResolveNature(entity.TransactionType)
            ?? throw new InvalidOperationException("El tipo de transacción no admite una política por cámara.");
        entity.RequiresPrenotification =
            entity.PrenotificationMode == PrenotificationRequirementMode.Mandatory;
        entity.AppliesToNachaExport = true;
        entity.AppliesToMonetaryTransactions = true;
    }

    private static ClearingHouseTransactionRuleDto Map(ClearingHouseTransactionRule rule)
        => new(
            rule.Id,
            rule.ClearingHouseId,
            rule.ClearingHouse?.Name ?? string.Empty,
            rule.TransactionNature,
            rule.TransactionType,
            rule.RequiresPrenotification,
            rule.PrenotificationMode,
            rule.PrenotificationLeadBusinessDays,
            rule.RequiresReceiverIdentificationValidation,
            rule.ReceiverIdentificationValidationMode,
            rule.AppliesToNachaExport,
            rule.AppliesToMonetaryTransactions,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.IsActive,
            rule.NormativeSource,
            rule.NormativeReference,
            rule.Notes,
            rule.CreatedAt,
            rule.UpdatedAt);
}
