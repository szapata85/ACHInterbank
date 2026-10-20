using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ClearingHouseSpecialDateService : IClearingHouseSpecialDateService
{
    private readonly AchDbContext _context;
    private readonly IOperationalCalendarService _calendar;
    private readonly ICycleCalendarGuard _cycleCalendarGuard;
    private readonly IOperationalTimeSnapshotProvider _operationalTime;

    public ClearingHouseSpecialDateService(
        AchDbContext context,
        IOperationalCalendarService? calendar = null,
        ICycleCalendarGuard? cycleCalendarGuard = null,
        IOperationalTimeSnapshotProvider? operationalTime = null)
    {
        _context = context;
        _calendar = calendar ?? new OperationalCalendarService(context);
        _cycleCalendarGuard = cycleCalendarGuard ?? new CycleCalendarGuard(context, _calendar);
        _operationalTime = operationalTime ?? new OperationalTimeSnapshotProvider();
    }

    public async Task<IReadOnlyList<ClearingHouseSpecialDateDto>> GetAllAsync(int? year, int? clearingHouseId, CancellationToken ct = default)
    {
        IQueryable<ClearingHouseSpecialDate> query = _context.ClearingHouseSpecialDates
            .AsNoTracking()
            .Include(d => d.ClearingHouse);

        if (year.HasValue)
        {
            query = query.Where(d => d.Date.Year == year);
        }

        if (clearingHouseId.HasValue)
        {
            query = query.Where(d => d.ClearingHouseId == clearingHouseId.Value);
        }

        var entities = await query
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        var holidayDates = new HashSet<DateOnly>();
        foreach (var calendarYear in entities.Select(x => x.Date.Year).Distinct())
        {
            holidayDates.UnionWith((await _calendar.GetNationalHolidaysAsync(calendarYear, ct)).Select(x => x.Date));
        }

        return entities.Select(entity => ToDto(entity, holidayDates.Contains(entity.Date))).ToArray();
    }

    public async Task<ClearingHouseSpecialDateDto> CreateAsync(ClearingHouseSpecialDateDto dto, CancellationToken ct = default)
    {
        await EnsureClearingHouseExists(dto.ClearingHouseId, ct);
        var date = DateOnly.FromDateTime(dto.Date);
        await EnsureNoDuplicateAsync(0, dto.ClearingHouseId, date, ct);

        var entity = new ClearingHouseSpecialDate
        {
            ClearingHouseId = dto.ClearingHouseId,
            Date = date,
            Description = NormalizeDescription(dto.Description),
            IsActive = dto.IsActive
        };

        _context.ClearingHouseSpecialDates.Add(entity);
        await _context.SaveChangesAsync(ct);
        if (entity.IsActive)
        {
            await ReevaluatePendingCyclesAsync(entity, ct);
        }

        var nationalHoliday = await _calendar.IsNationalHolidayAsync(entity.Date, ct);
        return ToDto(entity, nationalHoliday);
    }

    public async Task<ClearingHouseSpecialDateDto> UpdateAsync(ClearingHouseSpecialDateDto dto, CancellationToken ct = default)
    {
        var entity = await _context.ClearingHouseSpecialDates
            .Include(d => d.ClearingHouse)
            .FirstOrDefaultAsync(d => d.Id == dto.Id, ct)
            ?? throw new InvalidOperationException("Fecha especial no encontrada.");

        await EnsureClearingHouseExists(dto.ClearingHouseId, ct);
        if (entity.ClearingHouseId != dto.ClearingHouseId)
        {
            throw new InvalidOperationException("Una fecha especial no se puede trasladar a otra cámara; cree una configuración independiente.");
        }

        var date = DateOnly.FromDateTime(dto.Date);
        await EnsureNoDuplicateAsync(entity.Id, dto.ClearingHouseId, date, ct);

        entity.Date = date;
        entity.Description = NormalizeDescription(dto.Description);
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(ct);
        if (entity.IsActive)
        {
            await ReevaluatePendingCyclesAsync(entity, ct);
        }

        var nationalHoliday = await _calendar.IsNationalHolidayAsync(entity.Date, ct);
        return ToDto(entity, nationalHoliday);
    }

    public async Task<ClearingHouseSpecialDateDto> ChangeStatusAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var entity = await _context.ClearingHouseSpecialDates.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
        {
            throw new InvalidOperationException("Fecha especial no encontrada.");
        }

        entity.IsActive = isActive;
        await _context.SaveChangesAsync(ct);
        if (entity.IsActive)
        {
            await ReevaluatePendingCyclesAsync(entity, ct);
        }
        var nationalHoliday = await _calendar.IsNationalHolidayAsync(entity.Date, ct);
        return ToDto(entity, nationalHoliday);
    }

    private async Task EnsureClearingHouseExists(int clearingHouseId, CancellationToken ct)
    {
        var exists = await _context.ClearingHouses.AnyAsync(ch => ch.Id == clearingHouseId, ct);
        if (!exists)
        {
            throw new InvalidOperationException("La cámara compensadora no existe.");
        }
    }

    private async Task ReevaluatePendingCyclesAsync(ClearingHouseSpecialDate specialDate, CancellationToken ct)
    {
        var today = _operationalTime.CaptureNow().OperationalDate;
        if (specialDate.Date < today)
        {
            return;
        }

        var date = specialDate.Date.ToDateTime(TimeOnly.MinValue).Date;
        var cycles = await _context.AchCycles
            .Where(x => x.ClearingHouseId == specialDate.ClearingHouseId
                        && x.ProcessingDate.Date == date
                        && x.RescheduleOnHoliday
                        && x.OperationalStatus != AchCycleOperationalStatus.Closed
                        && x.OperationalStatus != AchCycleOperationalStatus.Cancelled)
            .ToListAsync(ct);

        foreach (var cycle in cycles)
        {
            await _cycleCalendarGuard.EnsureExecutableAsync(cycle, ct);
        }
    }

    private async Task EnsureNoDuplicateAsync(int currentId, int clearingHouseId, DateOnly date, CancellationToken ct)
    {
        if (await _context.ClearingHouseSpecialDates.AnyAsync(
                x => x.Id != currentId && x.ClearingHouseId == clearingHouseId && x.Date == date,
                ct))
        {
            throw new InvalidOperationException("La fecha ya está configurada para esta cámara.");
        }
    }

    private static string NormalizeDescription(string? value)
    {
        var description = value?.Trim() ?? string.Empty;
        if (description.Length == 0)
        {
            throw new InvalidOperationException("El motivo de la fecha especial es obligatorio.");
        }

        return description.Length <= 200 ? description : description[..200];
    }

    private static ClearingHouseSpecialDateDto ToDto(ClearingHouseSpecialDate entity, bool isNationalHoliday)
    {
        var isWeekend = entity.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var warnings = new List<string>(2);
        if (isWeekend)
        {
            warnings.Add("La fecha ya corresponde a un sábado o domingo.");
        }
        if (isNationalHoliday)
        {
            warnings.Add("La fecha ya corresponde a un festivo nacional.");
        }

        return new ClearingHouseSpecialDateDto
        {
            Id = entity.Id,
            ClearingHouseId = entity.ClearingHouseId,
            ClearingHouseName = entity.ClearingHouse?.Name,
            Date = entity.Date.ToDateTime(TimeOnly.MinValue),
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsWeekend = isWeekend,
            IsNationalHoliday = isNationalHoliday,
            CalendarWarning = warnings.Count == 0 ? null : string.Join(" ", warnings)
        };
    }
}
