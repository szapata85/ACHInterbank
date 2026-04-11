using Cfa.ACHInterbank.Application.ACH.Interfaces;
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

    public ClearingHouseCycleConfigService(AchDbContext context)
    {
        _context = context;
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

        var baseQuery = _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .Where(c => c.ClearingHouseId == clearingHouseId &&
                        c.IsActive &&
                        c.EffectiveFrom.Date <= effectiveDate.Date &&
                        (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= effectiveDate.Date));

        var grouped = baseQuery
            .GroupBy(c => c.CycleName)
            .Select(g => g.OrderByDescending(c => c.EffectiveFrom).ThenByDescending(c => c.Id).First());

        var configs = await grouped
            .OrderBy(c => c.CutoffTime)
            .Select(c => MapToDto(c, effectiveDate))
            .ToListAsync(ct);

        return configs;
    }

    public async Task<ClearingHouseCycleConfigDto> CreateVersionAsync(UpsertClearingHouseCycleConfigDto dto, CancellationToken ct = default)
    {
        await EnsureClearingHouseExists(dto.ClearingHouseId, ct);
        ValidateDto(dto);

        var effectiveFrom = NormalizeRequiredUtcDate(dto.EffectiveFrom);
        var cycleName = dto.CycleName.Trim();

        await EnsureNoHistoricalOverlapAsync(dto.ClearingHouseId, cycleName, effectiveFrom, ct);

        var overlappingConfigs = await _context.ClearingHouseCycleConfigs
            .Where(c => c.ClearingHouseId == dto.ClearingHouseId && c.CycleName == cycleName &&
                        c.EffectiveFrom.Date <= effectiveFrom.Date &&
                        (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= effectiveFrom.Date))
            .ToListAsync(ct);

        foreach (var config in overlappingConfigs)
        {
            config.EffectiveTo = effectiveFrom.AddDays(-1);
            config.IsActive = false;
        }

        var entity = new ClearingHouseCycleConfig
        {
            ClearingHouseId = dto.ClearingHouseId,
            CycleName = cycleName,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            CutoffTime = dto.CutoffTime,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            IsActive = true
        };

        _context.ClearingHouseCycleConfigs.Add(entity);
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

    private static ClearingHouseCycleConfigDto MapToDto(ClearingHouseCycleConfig entity, DateTime? effectiveAt)
    {
        return new ClearingHouseCycleConfigDto
        {
            Id = entity.Id,
            ClearingHouseId = entity.ClearingHouseId,
            ClearingHouseName = entity.ClearingHouse?.Name,
            CycleName = entity.CycleName,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            CutoffTime = entity.CutoffTime,
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
}
