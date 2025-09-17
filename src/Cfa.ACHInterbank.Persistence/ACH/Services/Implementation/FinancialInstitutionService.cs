using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class FinancialInstitutionService : IFinancialInstitutionService
{
    private readonly AchDbContext _context;

    public FinancialInstitutionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FinancialInstitutionDto>> GetAllAsync()
    {
        //var result = Application.Helpers.DigitoChequeo.DigitoChequeo.CalcularDigitoChequeo("07640125");

        return await _context.FinancialInstitutions
            .Select(f => new FinancialInstitutionDto
            {
                Id = f.Id,
                Name = f.Name,
                Code = f.Code,
                ClearingHouseId = f.ClearingHouseId,
                IsDefaultSource = f.IsDefaultSource
            })
            .ToListAsync();
    }

    public async Task<FinancialInstitutionDto?> GetByIdAsync(int id)
    {
        return await _context.FinancialInstitutions
            .Where(x => x.Id == id)
            .Select(f => new FinancialInstitutionDto
            {
                Id = f.Id,
                Name = f.Name,
                Code = f.Code,
                ClearingHouseId = f.ClearingHouseId,
                IsDefaultSource = f.IsDefaultSource
            })
            .FirstOrDefaultAsync();
    }

    public async Task<FinancialInstitutionDto> CreateAsync(FinancialInstitutionDto dto)
    {
        bool exists = await _context.ClearingHouses.AnyAsync(ch => ch.Id == dto.ClearingHouseId);
        if (!exists) throw new InvalidOperationException("ClearingHouse no encontrado.");

        if (dto.IsDefaultSource)
        {
            var all = await _context.FinancialInstitutions.ToListAsync();
            foreach (var fi in all) fi.IsDefaultSource = false;
        }
        else
        {
            bool hasDefault = await _context.FinancialInstitutions.AnyAsync(fi => fi.IsDefaultSource);
            if (!hasDefault) dto.IsDefaultSource = true;
        }

        var entity = new FinancialInstitution
        {
            Name = dto.Name,
            Code = dto.Code,
            ClearingHouseId = dto.ClearingHouseId,
            IsDefaultSource = dto.IsDefaultSource
        };

        _context.FinancialInstitutions.Add(entity);
        await _context.SaveChangesAsync();
        dto.Id = entity.Id;
        return dto;
    }

    public async Task UpdateAsync(int id, FinancialInstitutionDto dto)
    {
        var entity = await _context.FinancialInstitutions.FindAsync(id)
            ?? throw new KeyNotFoundException("Institución no encontrada.");

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.ClearingHouseId = dto.ClearingHouseId;

        if (dto.IsDefaultSource)
        {
            var all = await _context.FinancialInstitutions.ToListAsync();
            foreach (var fi in all)
                fi.IsDefaultSource = fi.Id == id;
        }
        else
        {
            bool otherDefault = await _context.FinancialInstitutions
                .AnyAsync(fi => fi.Id != id && fi.IsDefaultSource);
            if (!otherDefault)
                throw new InvalidOperationException("Debe existir al menos una entidad financiera por defecto.");
            entity.IsDefaultSource = false;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.FinancialInstitutions.FindAsync(id)
            ?? throw new KeyNotFoundException("Institución no encontrada.");

        if (entity.IsDefaultSource)
        {
            bool otherDefault = await _context.FinancialInstitutions
                .AnyAsync(fi => fi.Id != id && fi.IsDefaultSource);
            if (!otherDefault)
                throw new InvalidOperationException("No se puede eliminar la única entidad predeterminada.");
        }

        _context.FinancialInstitutions.Remove(entity);
        await _context.SaveChangesAsync();
    }
}

