using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class BankHolidayAdminService : IBankHolidayAdminService
{
    private readonly AchDbContext _context;

    public BankHolidayAdminService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BankHolidayDto>> GetAllAsync(int? year, CancellationToken ct = default)
    {
        var query = _context.BankHolidays.AsNoTracking();

        if (year.HasValue)
        {
            query = query.Where(h => h.Date.Year == year);
        }

        var items = await query
            .OrderBy(h => h.Date)
            .Select(h => new BankHolidayDto
            {
                Id = h.Id,
                Date = h.Date.ToDateTime(TimeOnly.MinValue),
                CommemorativeDate = h.CommemorativeDate.HasValue
                    ? h.CommemorativeDate.Value.ToDateTime(TimeOnly.MinValue)
                    : null,
                Description = h.Description,
                CountryCode = h.CountryCode,
                RuleCode = h.RuleCode,
                RuleKind = h.RuleKind.HasValue ? h.RuleKind.Value.ToString() : null,
                IsSystemGenerated = h.IsSystemGenerated,
                LegalOrigin = h.LegalOrigin,
                EffectiveFromYear = h.EffectiveFromYear
            })
            .ToListAsync(ct);

        return items;
    }

    public async Task<BankHolidayDto> CreateAsync(BankHolidayDto dto, CancellationToken ct = default)
    {
        var date = DateOnly.FromDateTime(dto.Date);
        var countryCode = NormalizeCountryCode(dto.CountryCode);
        if (await _context.BankHolidays.AnyAsync(x => x.Date == date && x.CountryCode == countryCode, ct))
        {
            throw new InvalidOperationException("Ya existe un festivo nacional para la fecha indicada.");
        }

        var entity = new BankHolidayModel
        {
            Date = date,
            CommemorativeDate = date,
            Description = NormalizeDescription(dto.Description),
            CountryCode = countryCode,
            IsSystemGenerated = false
        };

        _context.BankHolidays.Add(entity);
        await _context.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<BankHolidayDto> UpdateAsync(BankHolidayDto dto, CancellationToken ct = default)
    {
        var entity = await _context.BankHolidays.FirstOrDefaultAsync(h => h.Id == dto.Id, ct)
            ?? throw new InvalidOperationException("Festivo no encontrado.");

        EnsureManual(entity);

        var date = DateOnly.FromDateTime(dto.Date);
        var countryCode = NormalizeCountryCode(dto.CountryCode);
        if (await _context.BankHolidays.AnyAsync(
                x => x.Id != entity.Id && x.Date == date && x.CountryCode == countryCode,
                ct))
        {
            throw new InvalidOperationException("Ya existe un festivo nacional para la fecha indicada.");
        }

        entity.Date = date;
        entity.CommemorativeDate = date;
        entity.Description = NormalizeDescription(dto.Description);
        entity.CountryCode = countryCode;

        await _context.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.BankHolidays.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (entity is null)
        {
            return;
        }

        EnsureManual(entity);

        _context.BankHolidays.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }

    private static void EnsureManual(BankHolidayModel entity)
    {
        if (entity.IsSystemGenerated)
        {
            throw new InvalidOperationException("Los festivos nacionales generados por la legislación no se pueden modificar ni eliminar manualmente.");
        }
    }

    private static string NormalizeCountryCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? "CO" : value.Trim().ToUpperInvariant();

    private static string NormalizeDescription(string? value)
    {
        var description = value?.Trim() ?? string.Empty;
        if (description.Length == 0)
        {
            throw new InvalidOperationException("El nombre del festivo es obligatorio.");
        }

        return description.Length <= 200 ? description : description[..200];
    }

    private static BankHolidayDto ToDto(BankHolidayModel entity)
        => new()
        {
            Id = entity.Id,
            Date = entity.Date.ToDateTime(TimeOnly.MinValue),
            CommemorativeDate = entity.CommemorativeDate?.ToDateTime(TimeOnly.MinValue),
            Description = entity.Description,
            CountryCode = entity.CountryCode,
            RuleCode = entity.RuleCode,
            RuleKind = entity.RuleKind?.ToString(),
            IsSystemGenerated = entity.IsSystemGenerated,
            LegalOrigin = entity.LegalOrigin,
            EffectiveFromYear = entity.EffectiveFromYear
        };
}
