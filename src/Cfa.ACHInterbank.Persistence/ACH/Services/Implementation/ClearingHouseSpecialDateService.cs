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

    public ClearingHouseSpecialDateService(AchDbContext context)
    {
        _context = context;
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

        var items = await query
            .OrderBy(d => d.Date)
            .Select(d => new ClearingHouseSpecialDateDto
            {
                Id = d.Id,
                ClearingHouseId = d.ClearingHouseId,
                ClearingHouseName = d.ClearingHouse.Name,
                Date = d.Date.ToDateTime(TimeOnly.MinValue),
                Description = d.Description
                ,IsActive = d.IsActive
            })
            .ToListAsync(ct);

        return items;
    }

    public async Task<ClearingHouseSpecialDateDto> CreateAsync(ClearingHouseSpecialDateDto dto, CancellationToken ct = default)
    {
        await EnsureClearingHouseExists(dto.ClearingHouseId, ct);

        var entity = new ClearingHouseSpecialDate
        {
            ClearingHouseId = dto.ClearingHouseId,
            Date = DateOnly.FromDateTime(dto.Date),
            Description = dto.Description
            ,IsActive = dto.IsActive
        };

        _context.ClearingHouseSpecialDates.Add(entity);
        await _context.SaveChangesAsync(ct);

        dto.Id = entity.Id;
        dto.Date = entity.Date.ToDateTime(TimeOnly.MinValue);

        return dto;
    }

    public async Task<ClearingHouseSpecialDateDto> UpdateAsync(ClearingHouseSpecialDateDto dto, CancellationToken ct = default)
    {
        var entity = await _context.ClearingHouseSpecialDates
            .Include(d => d.ClearingHouse)
            .FirstOrDefaultAsync(d => d.Id == dto.Id, ct)
            ?? throw new InvalidOperationException("Fecha especial no encontrada.");

        await EnsureClearingHouseExists(dto.ClearingHouseId, ct);

        entity.ClearingHouseId = dto.ClearingHouseId;
        entity.Date = DateOnly.FromDateTime(dto.Date);
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(ct);

        dto.Date = entity.Date.ToDateTime(TimeOnly.MinValue);
        dto.ClearingHouseName = entity.ClearingHouse.Name;

        return dto;
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
        return new ClearingHouseSpecialDateDto
        {
            Id = entity.Id,
            ClearingHouseId = entity.ClearingHouseId,
            Date = entity.Date.ToDateTime(TimeOnly.MinValue),
            Description = entity.Description,
            IsActive = entity.IsActive
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
}
