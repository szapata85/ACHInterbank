using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ClearingHouseCycleConfigService : IClearingHouseCycleConfigService
{
    private readonly AchDbContext _context;
    private readonly ICycleNumberResolver _cycleNumberResolver;

    public ClearingHouseCycleConfigService(AchDbContext context, ICycleNumberResolver? cycleNumberResolver = null)
    {
        _context = context;
        _cycleNumberResolver = cycleNumberResolver ?? new CycleNumberResolver();
    }

    public async Task<IReadOnlyList<ClearingHouseCycleConfigDto>> GetByClearingHouseAsync(int clearingHouseId, DateTime? effectiveAt, CancellationToken ct = default)
    {
        await EnsureClearingHouseExists(clearingHouseId, ct);

        var effectiveDate = NormalizeUtcDate(effectiveAt);

        var query = _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .Where(c => c.ClearingHouseId == clearingHouseId);

        if (effectiveDate.HasValue)
        {
            query = query.Where(c => c.EffectiveFrom.Date <= effectiveDate.Value.Date &&
                                     (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= effectiveDate.Value.Date));
        }

        var configs = await query
            .OrderBy(c => c.CycleName)
            .ThenByDescending(c => c.EffectiveFrom)
            .Select(c => MapToDto(c, effectiveDate))
            .ToListAsync(ct);

        return configs;
    }

    public async Task<IReadOnlyList<ClearingHouseCycleConfigDto>> GetCurrentByClearingHouseAsync(int clearingHouseId, DateTime? effectiveAt, CancellationToken ct = default)
    {
        await EnsureClearingHouseExists(clearingHouseId, ct);

        var effectiveDate = NormalizeUtcDate(effectiveAt) ?? DateTime.UtcNow.Date;

        // EF provider compatibility note:
        // SQLite fails translating GroupBy(...).Select(First...).OrderBy(TimeSpan),
        // so we materialize the filtered slice (single clearing house + date window)
        // and then apply grouping/ordering in-memory. Dataset is bounded/configuration-sized.
        var filteredConfigs = await _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .Where(c => c.ClearingHouseId == clearingHouseId &&
                        c.IsActive &&
                        c.EffectiveFrom.Date <= effectiveDate.Date &&
                        (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= effectiveDate.Date))
            .ToListAsync(ct);

        var versions = filteredConfigs
            .GroupBy(c => NormalizePolicyVersion(c.PolicyVersion), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (versions.Count > 1)
        {
            throw new InvalidOperationException("Existen múltiples versiones de política de ciclos vigentes para la misma cámara y fecha.");
        }

        var configs = versions.SelectMany(group => group)
            .OrderBy(c => c.CutoffTime)
            .Select(c => MapToDto(c, effectiveDate))
            .ToList();

        return configs;
    }

    public async Task<ClearingHouseCycleConfigDto> CreateVersionAsync(UpsertClearingHouseCycleConfigDto dto, CancellationToken ct = default)
    {
        await EnsureClearingHouseExists(dto.ClearingHouseId, ct);
        ValidateDto(dto);

        var effectiveFrom = NormalizeRequiredUtcDate(dto.EffectiveFrom);
        var cycleName = dto.CycleName.Trim();
        await EnsureNoHistoricalOverlapAsync(dto.ClearingHouseId, cycleName, effectiveFrom, ct);
        if (await _context.ClearingHouseCycleConfigs.AnyAsync(c =>
                c.ClearingHouseId == dto.ClearingHouseId
                && c.EffectiveFrom.Date == effectiveFrom.Date, ct))
        {
            throw new InvalidOperationException("Ya existe una versión de política con la misma vigencia inicial.");
        }

        var sourceDate = effectiveFrom.AddDays(-1);
        var source = await _context.ClearingHouseCycleConfigs
            .Where(c => c.ClearingHouseId == dto.ClearingHouseId
                && c.IsActive
                && c.EffectiveFrom.Date <= sourceDate.Date
                && (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= sourceDate.Date))
            .ToListAsync(ct);
        var sourceVersions = source
            .GroupBy(c => NormalizePolicyVersion(c.PolicyVersion), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (sourceVersions.Count > 1)
        {
            throw new InvalidOperationException("La política predecesora es ambigua y no puede versionarse.");
        }

        var policyVersion = string.IsNullOrWhiteSpace(dto.PolicyVersion)
            ? $"V{effectiveFrom:yyyyMMdd}"
            : dto.PolicyVersion.Trim();
        var targetNumber = _cycleNumberResolver.Resolve(cycleName);
        var inherited = source.SingleOrDefault(config =>
            string.Equals(config.CycleName.Trim(), cycleName, StringComparison.OrdinalIgnoreCase)
            || targetNumber.HasValue && _cycleNumberResolver.Resolve(config.CycleName) == targetNumber);
        var entity = CreateConfiguredCycle(
            dto.ClearingHouseId,
            policyVersion,
            cycleName,
            dto,
            inherited,
            effectiveFrom);

        var nextPolicyStart = await _context.ClearingHouseCycleConfigs
            .Where(c => c.ClearingHouseId == dto.ClearingHouseId && c.EffectiveFrom.Date > effectiveFrom.Date)
            .Select(c => (DateTime?)c.EffectiveFrom)
            .OrderBy(date => date)
            .FirstOrDefaultAsync(ct);
        entity.EffectiveTo = nextPolicyStart?.AddDays(-1);

        var newPolicy = source
            .Where(config => config.Id != inherited?.Id)
            .Select(config => CloneForPolicy(config, policyVersion, effectiveFrom, entity.EffectiveTo))
            .Append(entity)
            .ToList();
        ValidatePolicyCycles(newPolicy);

        foreach (var config in source.Where(config => !config.EffectiveTo.HasValue || config.EffectiveTo.Value.Date >= effectiveFrom.Date))
        {
            config.EffectiveTo = effectiveFrom.AddDays(-1);
        }

        _context.ClearingHouseCycleConfigs.AddRange(newPolicy);
        await _context.SaveChangesAsync(ct);

        var created = await _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .FirstAsync(c => c.Id == entity.Id, ct);

        return MapToDto(created, effectiveFrom);
    }

    public async Task<ClearingHouseCycleConfigDto> InactivateAsync(int id, DateTime effectiveTo, CancellationToken ct = default)
    {
        var entity = await _context.ClearingHouseCycleConfigs
            .Include(c => c.ClearingHouse)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Configuración de ciclo no encontrada.");

        var normalizedEffectiveTo = NormalizeRequiredUtcDate(effectiveTo);

        if (normalizedEffectiveTo.Date < entity.EffectiveFrom.Date)
        {
            throw new InvalidOperationException("La fecha de fin de vigencia no puede ser menor a la fecha de inicio.");
        }

        entity.EffectiveTo = normalizedEffectiveTo;
        entity.IsActive = false;
        await _context.SaveChangesAsync(ct);

        return MapToDto(entity, normalizedEffectiveTo);
    }

    public async Task<ClearingHouseCycleConfigDto> ChangeStatusAsync(
        int id,
        bool isActive,
        DateTime? effectiveTo,
        CancellationToken ct = default)
    {
        var entity = await _context.ClearingHouseCycleConfigs
            .Include(x => x.ClearingHouse)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Configuración de ciclo no encontrada.");

        if (entity.IsActive == isActive)
        {
            return MapToDto(entity, DateTime.UtcNow.Date);
        }

        if (!isActive)
        {
            return await InactivateAsync(id, effectiveTo ?? DateTime.UtcNow.Date, ct);
        }

        var duplicate = await _context.ClearingHouseCycleConfigs.AnyAsync(x =>
            x.Id != id && x.ClearingHouseId == entity.ClearingHouseId &&
            x.CycleName == entity.CycleName && x.IsActive, ct);
        if (duplicate)
        {
            throw new InvalidOperationException("Ya existe una versión activa del ciclo.");
        }

        entity.IsActive = true;
        entity.EffectiveTo = null;
        await _context.SaveChangesAsync(ct);
        return MapToDto(entity, DateTime.UtcNow.Date);
    }

    private static ClearingHouseCycleConfigDto MapToDto(ClearingHouseCycleConfig entity, DateTime? effectiveAt)
    {
        return new ClearingHouseCycleConfigDto
        {
            Id = entity.Id,
            ClearingHouseId = entity.ClearingHouseId,
            ClearingHouseName = entity.ClearingHouse?.Name,
            PolicyVersion = NormalizePolicyVersion(entity.PolicyVersion),
            CycleName = entity.CycleName,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            CutoffTime = entity.CutoffTime,
            OutputReleaseTime = entity.OutputReleaseTime,
            AllowsMonetaryCredit = entity.AllowsMonetaryCredit,
            AllowsMonetaryDebit = entity.AllowsMonetaryDebit,
            AllowsCreditPrenotification = entity.AllowsCreditPrenotification,
            AllowsDebitPrenotification = entity.AllowsDebitPrenotification,
            AllowsReturn = entity.AllowsReturn,
            AllowsReturnOfReturn = entity.AllowsReturnOfReturn,
            IsActive = entity.IsActive,
            EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo,
            IsCurrent = effectiveAt.HasValue &&
                        entity.EffectiveFrom.Date <= effectiveAt.Value.Date &&
                        (!entity.EffectiveTo.HasValue || entity.EffectiveTo.Value.Date >= effectiveAt.Value.Date)
        };
    }

    private async Task EnsureClearingHouseExists(int clearingHouseId, CancellationToken ct)
    {
        var exists = await _context.ClearingHouses.AnyAsync(ch => ch.Id == clearingHouseId, ct);
        if (!exists)
        {
            throw new InvalidOperationException("La cámara compensadora no existe.");
        }
    }

    private static void ValidateDto(UpsertClearingHouseCycleConfigDto dto)
    {
        if (dto.ClearingHouseId <= 0)
        {
            throw new InvalidOperationException("La cámara compensadora es requerida.");
        }

        if (string.IsNullOrWhiteSpace(dto.CycleName))
        {
            throw new InvalidOperationException("El nombre del ciclo es requerido.");
        }

        if (dto.StartTime == dto.EndTime)
        {
            throw new InvalidOperationException("La hora de inicio y fin del ciclo no pueden ser iguales.");
        }

        if (dto.CutoffTime == TimeSpan.Zero)
        {
            throw new InvalidOperationException("La hora de corte es requerida.");
        }

        if (dto.CutoffTime < TimeSpan.Zero || dto.CutoffTime > new TimeSpan(23, 59, 59))
        {
            throw new InvalidOperationException("La hora de corte está fuera del rango permitido.");
        }

        if (!IsTimeWithinWindow(dto.StartTime, dto.EndTime, dto.CutoffTime))
        {
            throw new InvalidOperationException("La hora de corte debe estar dentro de la ventana operativa configurada.");
        }

        var release = dto.OutputReleaseTime ?? dto.EndTime;
        var candidate = new ClearingHouseCycleConfig
        {
            CycleName = dto.CycleName,
            StartTime = dto.StartTime,
            CutoffTime = dto.CutoffTime,
            EndTime = dto.EndTime,
            OutputReleaseTime = release
        };
        ClearingHouseCyclePolicyResolver.ValidateStageOrder(candidate);
    }

    private async Task EnsureNoHistoricalOverlapAsync(int clearingHouseId, string cycleName, DateTime effectiveFrom, CancellationToken ct)
    {
        var conflictingHistoricalVersion = await _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .AnyAsync(c => c.ClearingHouseId == clearingHouseId &&
                           c.CycleName == cycleName &&
                           !c.IsActive &&
                           c.EffectiveFrom.Date <= effectiveFrom.Date &&
                           c.EffectiveTo.HasValue &&
                           c.EffectiveTo.Value.Date >= effectiveFrom.Date,
                ct);

        if (conflictingHistoricalVersion)
        {
            throw new InvalidOperationException("Existe traslape con una configuración histórica cerrada para esa vigencia.");
        }

        var duplicatedEffectiveFrom = await _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .AnyAsync(c => c.ClearingHouseId == clearingHouseId &&
                           c.CycleName == cycleName &&
                           c.EffectiveFrom.Date == effectiveFrom.Date,
                ct);

        if (duplicatedEffectiveFrom)
        {
            throw new InvalidOperationException("Ya existe una versión del ciclo con la misma vigencia inicial.");
        }
    }

    private static bool IsTimeWithinWindow(TimeSpan start, TimeSpan end, TimeSpan value)
    {
        if (start < end)
        {
            return value >= start && value <= end;
        }

        // Ventana que cruza medianoche.
        return value >= start || value <= end;
    }

    private static DateTime NormalizeRequiredUtcDate(DateTime date)
    {
        if (date == default)
        {
            throw new InvalidOperationException("La fecha de vigencia es requerida.");
        }

        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }

    private static DateTime? NormalizeUtcDate(DateTime? date)
    {
        return date.HasValue
            ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc)
            : null;
    }

    private static ClearingHouseCycleConfig CreateConfiguredCycle(
        int clearingHouseId,
        string policyVersion,
        string cycleName,
        UpsertClearingHouseCycleConfigDto dto,
        ClearingHouseCycleConfig? inherited,
        DateTime effectiveFrom)
        => new()
        {
            ClearingHouseId = clearingHouseId,
            PolicyVersion = policyVersion,
            CycleName = cycleName,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            CutoffTime = dto.CutoffTime,
            OutputReleaseTime = dto.OutputReleaseTime ?? inherited?.OutputReleaseTime ?? dto.EndTime,
            AllowsMonetaryCredit = dto.AllowsMonetaryCredit ?? inherited?.AllowsMonetaryCredit ?? true,
            AllowsMonetaryDebit = dto.AllowsMonetaryDebit ?? inherited?.AllowsMonetaryDebit ?? true,
            AllowsCreditPrenotification = dto.AllowsCreditPrenotification ?? inherited?.AllowsCreditPrenotification ?? true,
            AllowsDebitPrenotification = dto.AllowsDebitPrenotification ?? inherited?.AllowsDebitPrenotification ?? true,
            AllowsReturn = dto.AllowsReturn ?? inherited?.AllowsReturn ?? true,
            AllowsReturnOfReturn = dto.AllowsReturnOfReturn ?? inherited?.AllowsReturnOfReturn ?? true,
            EffectiveFrom = effectiveFrom,
            IsActive = true
        };

    private static ClearingHouseCycleConfig CloneForPolicy(
        ClearingHouseCycleConfig source,
        string policyVersion,
        DateTime effectiveFrom,
        DateTime? effectiveTo)
        => new()
        {
            ClearingHouseId = source.ClearingHouseId,
            PolicyVersion = policyVersion,
            CycleName = source.CycleName,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            CutoffTime = source.CutoffTime,
            OutputReleaseTime = source.OutputReleaseTime,
            AllowsMonetaryCredit = source.AllowsMonetaryCredit,
            AllowsMonetaryDebit = source.AllowsMonetaryDebit,
            AllowsCreditPrenotification = source.AllowsCreditPrenotification,
            AllowsDebitPrenotification = source.AllowsDebitPrenotification,
            AllowsReturn = source.AllowsReturn,
            AllowsReturnOfReturn = source.AllowsReturnOfReturn,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true
        };

    private void ValidatePolicyCycles(IReadOnlyCollection<ClearingHouseCycleConfig> cycles)
    {
        var duplicateName = cycles
            .GroupBy(cycle => cycle.CycleName.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateName is not null)
        {
            throw new InvalidOperationException($"El ciclo '{duplicateName.Key}' está duplicado en la política.");
        }

        var duplicateNumber = cycles
            .Select(cycle => _cycleNumberResolver.Resolve(cycle.CycleName))
            .Where(number => number.HasValue)
            .GroupBy(number => number!.Value)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateNumber is not null)
        {
            throw new InvalidOperationException($"El número de ciclo {duplicateNumber.Key} está duplicado en la política.");
        }

        foreach (var cycle in cycles)
        {
            ClearingHouseCyclePolicyResolver.ValidateStageOrder(cycle);
        }
    }

    private static string NormalizePolicyVersion(string? value)
        => string.IsNullOrWhiteSpace(value) ? "LEGACY" : value.Trim();
}
