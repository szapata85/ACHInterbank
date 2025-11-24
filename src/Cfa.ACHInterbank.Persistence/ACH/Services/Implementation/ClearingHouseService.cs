using AutoMapper;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.Configurations;
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

    public async Task<PaginatedResult<ClearingHouseDto>> GetAsync(PaginationRequest request, CancellationToken ct = default)
    {
        var query = _context.ClearingHouses
            .Include(ch => ch.ClearingHouseConfig)
            .AsNoTracking()
            .OrderBy(ch => ch.Name);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PaginatedResult<ClearingHouseDto>
        {
            Items = _mapper.Map<IEnumerable<ClearingHouseDto>>(items),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}
