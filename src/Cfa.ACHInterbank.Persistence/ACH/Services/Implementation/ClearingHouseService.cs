using AutoMapper;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ClearingHouseService : IClearingHouseService
{
    private readonly AchDbContext _context;
    private readonly IMapper _mapper;

    public ClearingHouseService(AchDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClearingHouseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _context.ClearingHouses
            .Include(ch => ch.ClearingHouseConfig)
            .AsNoTracking()
            .OrderBy(ch => ch.Name)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<ClearingHouseDto>>(entities);
    }

    public async Task<ClearingHouseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.ClearingHouses
            .Include(ch => ch.ClearingHouseConfig)
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.Id == id, ct);

        return _mapper.Map<ClearingHouseDto?>(entity);
    }
}
