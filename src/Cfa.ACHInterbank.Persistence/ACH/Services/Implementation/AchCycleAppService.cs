using AutoMapper;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchCycleAppService : IAchCycleAppService
{
    private readonly AchDbContext _context;
    private readonly IMapper _mapper;

    public AchCycleAppService(AchDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AchCycleDto>> GetAsync(int? clearingHouseId = null, DateTime? processingDate = null, CancellationToken ct = default)
    {
        var query = _context.AchCycles
            .AsNoTracking()
            .Include(cycle => cycle.ClearingHouse)
            .AsQueryable();

        if (clearingHouseId.HasValue)
        {
            query = query.Where(cycle => cycle.ClearingHouseId == clearingHouseId.Value);
        }

        if (processingDate.HasValue)
        {
            var dateOnly = processingDate.Value.Date;
            query = query.Where(cycle => cycle.ProcessingDate.Date == dateOnly);
        }

        var cycles = await query
            .OrderBy(cycle => cycle.ProcessingDate)
            .ThenBy(cycle => cycle.CutoffTime)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<AchCycleDto>>(cycles);
    }

    public async Task<AchCycleDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _context.AchCycles
            .AsNoTracking()
            .Include(cycle => cycle.ClearingHouse)
            .FirstOrDefaultAsync(cycle => cycle.Id == id, ct);

        return _mapper.Map<AchCycleDto?>(entity);
    }

    public async Task<AchCycleDto> CreateAsync(AchCycleRequest request, CancellationToken ct = default)
    {
        await ValidateClearingHouseAsync(request.ClearingHouseId, ct);

        var entity = _mapper.Map<AchCycle>(request);
        entity.Id = AchCycleIdHelper.GenerateId(request.ClearingHouseId, request.CycleName, request.ProcessingDate.Date);
        entity.ProcessingDate = entity.ProcessingDate.Date;

        _context.AchCycles.Add(entity);
        await _context.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task<AchCycleDto> UpdateAsync(string id, AchCycleRequest request, CancellationToken ct = default)
    {
        var entity = await _context.AchCycles
            .FirstOrDefaultAsync(cycle => cycle.Id == id, ct)
            ?? throw new KeyNotFoundException("Ciclo ACH no encontrado");

        await ValidateClearingHouseAsync(request.ClearingHouseId, ct);

        _mapper.Map(request, entity);
        entity.ProcessingDate = entity.ProcessingDate.Date;

        await _context.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _context.AchCycles.FirstOrDefaultAsync(cycle => cycle.Id == id, ct)
            ?? throw new KeyNotFoundException("Ciclo ACH no encontrado");

        _context.AchCycles.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<AchCycleExportDto>> GetExecutedWithTransactionsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        return await _context.AchCycles
            .AsNoTracking()
            .Where(cycle => cycle.ProcessingDate <= today)
            .Where(cycle => cycle.Transactions.Any())
            .Select(cycle => new AchCycleExportDto
            {
                Id = cycle.Id,
                CycleName = cycle.CycleName,
                ProcessingDate = cycle.ProcessingDate,
                ClearingHouseName = cycle.ClearingHouse != null ? cycle.ClearingHouse.Name : null,
                TransactionCount = cycle.Transactions.Count
            })
            .OrderByDescending(cycle => cycle.ProcessingDate)
            .ThenBy(cycle => cycle.Id)
            .ToListAsync(ct);
    }

    private async Task ValidateClearingHouseAsync(int clearingHouseId, CancellationToken ct)
    {
        var exists = await _context.ClearingHouses.AnyAsync(ch => ch.Id == clearingHouseId, ct);
        if (!exists)
        {
            throw new KeyNotFoundException("Cámara de compensación no encontrada");
        }
    }
}
