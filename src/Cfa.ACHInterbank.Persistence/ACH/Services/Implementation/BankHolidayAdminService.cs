using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
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
                Description = h.Description,
                CountryCode = h.CountryCode
            })
            .ToListAsync(ct);

        return items;
    }

    public async Task<BankHolidayDto> CreateAsync(BankHolidayDto dto, CancellationToken ct = default)
    {
        var entity = new BankHolidayModel
        {
            Date = DateOnly.FromDateTime(dto.Date),
            Description = dto.Description,
            CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? "CO" : dto.CountryCode
        };

        _context.BankHolidays.Add(entity);
        await _context.SaveChangesAsync(ct);

        dto.Id = entity.Id;
        dto.Date = entity.Date.ToDateTime(TimeOnly.MinValue);
        dto.CountryCode = entity.CountryCode;

        return dto;
    }

    public async Task<BankHolidayDto> UpdateAsync(BankHolidayDto dto, CancellationToken ct = default)
    {
        var entity = await _context.BankHolidays.FirstOrDefaultAsync(h => h.Id == dto.Id, ct)
            ?? throw new InvalidOperationException("Festivo no encontrado.");

        entity.Date = DateOnly.FromDateTime(dto.Date);
        entity.Description = dto.Description;
        entity.CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? "CO" : dto.CountryCode;

        await _context.SaveChangesAsync(ct);

        dto.Date = entity.Date.ToDateTime(TimeOnly.MinValue);
        dto.CountryCode = entity.CountryCode;

        return dto;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.BankHolidays.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (entity is null)
        {
            return;
        }

        _context.BankHolidays.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }
}
