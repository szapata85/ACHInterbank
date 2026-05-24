using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
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

    public async Task<IReadOnlyList<ClearingHouseTransactionRuleDto>> GetAsync(int? clearingHouseId, string? transactionNature, bool includeInactive, CancellationToken ct)
    {
        var query = _context.ClearingHouseTransactionRules
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .AsQueryable();

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

        var rules = await query
            .OrderBy(x => x.ClearingHouse.Name)
            .ThenBy(x => x.TransactionNature)
            .ThenBy(x => x.TransactionType)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(ct);

        return rules.Select(Map).ToList();
    }

    public async Task<ClearingHouseTransactionRuleDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var rule = await _context.ClearingHouseTransactionRules
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(ct);

        return rule is null ? null : Map(rule);
    }

    public async Task<ClearingHouseTransactionRuleDto> CreateAsync(CreateClearingHouseTransactionRuleRequest request, CancellationToken ct)
    {
        await ValidateRequestAsync(request.ClearingHouseId, request.TransactionNature, request.TransactionType, request.EffectiveFrom, request.EffectiveTo, request.NormativeSource, request.NormativeReference, null, ct);

        var entity = new ClearingHouseTransactionRule
        {
            ClearingHouseId = request.ClearingHouseId,
            TransactionNature = request.TransactionNature,
            TransactionType = request.TransactionType,
            RequiresPrenotification = request.RequiresPrenotification,
            PrenotificationMode = request.PrenotificationMode,
            RequiresReceiverIdentificationValidation = request.RequiresReceiverIdentificationValidation,
            ReceiverIdentificationValidationMode = request.ReceiverIdentificationValidationMode,
            AppliesToNachaExport = request.AppliesToNachaExport,
            AppliesToMonetaryTransactions = request.AppliesToMonetaryTransactions,
            EffectiveFrom = request.EffectiveFrom.Date,
            EffectiveTo = request.EffectiveTo?.Date,
            NormativeSource = request.NormativeSource.Trim(),
            NormativeReference = request.NormativeReference.Trim(),
            Notes = request.Notes?.Trim() ?? string.Empty,
            IsActive = true
        };

        _context.ClearingHouseTransactionRules.Add(entity);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar la regla creada.");
    }

    public async Task<ClearingHouseTransactionRuleDto> UpdateAsync(int id, UpdateClearingHouseTransactionRuleRequest request, CancellationToken ct)
    {
        var entity = await _context.ClearingHouseTransactionRules.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("La regla de cámara no existe.");

        await ValidateRequestAsync(request.ClearingHouseId, request.TransactionNature, request.TransactionType, request.EffectiveFrom, request.EffectiveTo, request.NormativeSource, request.NormativeReference, id, ct);

        entity.ClearingHouseId = request.ClearingHouseId;
        entity.TransactionNature = request.TransactionNature;
        entity.TransactionType = request.TransactionType;
        entity.RequiresPrenotification = request.RequiresPrenotification;
        entity.PrenotificationMode = request.PrenotificationMode;
        entity.RequiresReceiverIdentificationValidation = request.RequiresReceiverIdentificationValidation;
        entity.ReceiverIdentificationValidationMode = request.ReceiverIdentificationValidationMode;
        entity.AppliesToNachaExport = request.AppliesToNachaExport;
        entity.AppliesToMonetaryTransactions = request.AppliesToMonetaryTransactions;
        entity.EffectiveFrom = request.EffectiveFrom.Date;
        entity.EffectiveTo = request.EffectiveTo?.Date;
        entity.NormativeSource = request.NormativeSource.Trim();
        entity.NormativeReference = request.NormativeReference.Trim();
        entity.Notes = request.Notes?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar la regla actualizada.");
    }

    public async Task<ClearingHouseTransactionRuleDto> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var entity = await _context.ClearingHouseTransactionRules.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("La regla de cámara no existe.");

        entity.IsActive = isActive;
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar la regla actualizada.");
    }

    private async Task ValidateRequestAsync(
        int clearingHouseId,
        TransactionNature nature,
        Domain.Entities.Transactions.Enums.TransactionTypeEnum transactionType,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        string normativeSource,
        string normativeReference,
        int? currentId,
        CancellationToken ct)
    {
        if (!await _context.ClearingHouses.AnyAsync(x => x.Id == clearingHouseId, ct))
        {
            throw new InvalidOperationException("La cámara de compensación no existe.");
        }

        if (string.IsNullOrWhiteSpace(normativeSource))
        {
            throw new InvalidOperationException("La fuente normativa es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(normativeReference))
        {
            throw new InvalidOperationException("La referencia normativa es obligatoria.");
        }

        if (effectiveTo.HasValue && effectiveTo.Value.Date < effectiveFrom.Date)
        {
            throw new InvalidOperationException("La vigencia hasta no puede ser menor que la vigencia desde.");
        }

        var fromDate = effectiveFrom.Date;
        var toDate = effectiveTo?.Date ?? DateTime.MaxValue.Date;

        var overlapExists = await _context.ClearingHouseTransactionRules.AnyAsync(x =>
            x.Id != (currentId ?? 0)
            && x.IsActive
            && x.ClearingHouseId == clearingHouseId
            && x.TransactionNature == nature
            && x.TransactionType == transactionType
            && x.AppliesToNachaExport
            && x.AppliesToMonetaryTransactions
            && x.EffectiveFrom <= toDate
            && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= fromDate), ct);

        if (overlapExists)
        {
            throw new InvalidOperationException("Ya existe una regla activa solapada para la cámara, naturaleza y tipo de transacción.");
        }
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
