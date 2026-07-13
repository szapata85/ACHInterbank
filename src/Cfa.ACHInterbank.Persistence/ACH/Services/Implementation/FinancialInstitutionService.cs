using AutoMapper;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class FinancialInstitutionService : IFinancialInstitutionService
{
    private readonly AchDbContext _context;
    private readonly IMapper _mapper; // si usas AutoMapper

    public FinancialInstitutionService(AchDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<FinancialInstitutionDto>> GetAllAsync(
        bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _context.FinancialInstitutions.AsNoTracking();
        if (!includeInactive)
            query = query.Where(fi => fi.Status == FinancialInstitutionStatus.Active);

        var entities = await query.ToListAsync(ct);
        return _mapper.Map<IEnumerable<FinancialInstitutionDto>>(entities);
    }

    public async Task<FinancialInstitutionDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.FinancialInstitutions
            .AsNoTracking()
            .FirstOrDefaultAsync(fi => fi.Id == id, ct);
        return _mapper.Map<FinancialInstitutionDto?>(entity);
    }

    public async Task<FinancialInstitutionDto> CreateAsync(FinancialInstitutionDto dto, CancellationToken ct = default)
    {
        var entity = _mapper.Map<FinancialInstitution>(dto);
        entity.CalculateCheckDigit();

        _context.FinancialInstitutions.Add(entity);
        await _context.SaveChangesAsync(ct);

        return _mapper.Map<FinancialInstitutionDto>(entity);
    }

    public async Task<FinancialInstitutionDto> UpdateAsync(FinancialInstitutionDto dto, CancellationToken ct = default)
    {
        var entity = await _context.FinancialInstitutions.FindAsync(new object?[] { dto.Id }, ct)
                     ?? throw new KeyNotFoundException("Institución no encontrada");

        _mapper.Map(dto, entity);
        entity.CalculateCheckDigit();

        await _context.SaveChangesAsync(ct);
        return _mapper.Map<FinancialInstitutionDto>(entity);
    }

    public async Task SetStatusAsync(int id, FinancialInstitutionStatus newStatus, CancellationToken ct = default)
    {
        var entity = await _context.FinancialInstitutions.FindAsync(new object?[] { id }, ct)
                     ?? throw new KeyNotFoundException("Institución no encontrada");
        entity.Status = newStatus;
        await _context.SaveChangesAsync(ct);
    }

}

