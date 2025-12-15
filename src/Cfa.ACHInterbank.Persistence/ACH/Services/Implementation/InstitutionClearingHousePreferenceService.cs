using AutoMapper;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Persistence.DataBase;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class InstitutionClearingHousePreferenceService : IInstitutionClearingHousePreferenceService
{
    private readonly AchDbContext _context;
    private readonly IMapper _mapper;

    public InstitutionClearingHousePreferenceService(AchDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<InstitutionClearingHousePreferenceDto>> GetAllAsync(CancellationToken ct = default)
    {
        var preferences = await _context.InstitutionClearingHousePreferences
            .AsNoTracking()
            .Include(x => x.FinancialInstitution)
            .Include(x => x.ClearingHouse)
            .OrderBy(x => x.FinancialInstitution.Name)
            .ThenBy(x => x.Priority)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<InstitutionClearingHousePreferenceDto>>(preferences);
    }

    public async Task<InstitutionClearingHousePreferenceDto> UpdateAsync(
        InstitutionClearingHousePreferenceDto dto,
        CancellationToken ct = default)
    {
        var entity = await _context.InstitutionClearingHousePreferences
                         .Include(x => x.FinancialInstitution)
                         .Include(x => x.ClearingHouse)
                         .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                     ?? throw new KeyNotFoundException("Preferencia no encontrada");

        entity.Priority = dto.Priority;
        entity.IsDefault = dto.IsDefault;

        await _context.SaveChangesAsync(ct);

        return _mapper.Map<InstitutionClearingHousePreferenceDto>(entity);
    }
}
