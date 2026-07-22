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

    public async Task<IEnumerable<AchCycleDto>> GetAsync(
        int? clearingHouseId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var query = _context.AchCycles
            .AsNoTracking()
            .Include(cycle => cycle.ClearingHouse)
            .AsQueryable();

        if (clearingHouseId.HasValue)
        {
            query = query.Where(cycle => cycle.ClearingHouseId == clearingHouseId.Value);
        }

        if (startDate.HasValue || endDate.HasValue)
        {
            var start = startDate?.Date;
            var end = endDate?.Date;

            if (start.HasValue && end.HasValue && start > end)
            {
                (start, end) = (end, start);
            }

            if (start.HasValue)
            {
                query = query.Where(cycle => cycle.ProcessingDate.Date >= start.Value);
            }

            if (end.HasValue)
            {
                query = query.Where(cycle => cycle.ProcessingDate.Date <= end.Value);
            }
        }

        var cycles = await query
            .OrderBy(cycle => cycle.ProcessingDate)
            .ThenBy(cycle => cycle.CutoffTime)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<AchCycleDto>>(cycles)
            .Zip(cycles, ApplyOperationalMetadata);
    }

    public async Task<AchCycleDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _context.AchCycles
            .AsNoTracking()
            .Include(cycle => cycle.ClearingHouse)
            .FirstOrDefaultAsync(cycle => cycle.Id == id, ct);

        var dto = _mapper.Map<AchCycleDto?>(entity);
        return dto is null || entity is null ? dto : ApplyOperationalMetadata(dto, entity);
    }

    public async Task<AchCycleDto> CreateAsync(AchCycleRequest request, CancellationToken ct = default)
    {
        await ValidateClearingHouseAsync(request.ClearingHouseId, ct);

        var entity = _mapper.Map<AchCycle>(request);
        entity.Id = AchCycleIdHelper.GenerateId(request.ClearingHouseId, request.CycleName, request.ProcessingDate.Date);
        entity.ProcessingDate = entity.ProcessingDate.Date;
        entity.ClearingHouseCycleConfigId = await ResolveCycleConfigurationIdAsync(request, ct);

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
        entity.ClearingHouseCycleConfigId = await ResolveCycleConfigurationIdAsync(request, ct);

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

    public async Task<IEnumerable<AchCycleExportDto>> GetExecutedWithTransactionsAsync(
        int? clearingHouseId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var query = _context.AchCycles
            .AsNoTracking()
            .Where(cycle => _context.AchTransactions.Any(transaction =>
                transaction.AchCycleId == cycle.Id
                && NachaExportEligibility.ExportableStates.Contains(transaction.State)))
            .AsQueryable();

        if (clearingHouseId.HasValue)
        {
            query = query.Where(cycle => cycle.ClearingHouseId == clearingHouseId.Value);
        }

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(cycle => _context.AchTransactions.Any(transaction =>
                transaction.AchCycleId == cycle.Id && transaction.EffectiveEntryDate >= start));
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date;
            query = query.Where(cycle => _context.AchTransactions.Any(transaction =>
                transaction.AchCycleId == cycle.Id && transaction.EffectiveEntryDate <= end));
        }

        return await query
            .Select(cycle => new AchCycleExportDto
            {
                Id = cycle.Id,
                CycleId = cycle.Id,
                ExportIdentifier = cycle.Id,
                CycleName = cycle.CycleName,
                ProcessingDate = cycle.ProcessingDate,
                ClearingHouseName = cycle.ClearingHouse != null ? cycle.ClearingHouse.Name : null,
                TransactionCount = _context.AchTransactions.Count(transaction =>
                    transaction.AchCycleId == cycle.Id
                    && NachaExportEligibility.ExportableStates.Contains(transaction.State)),
                IsExportable = _context.AchBatches.Any(batch => batch.AchCycleId == cycle.Id)
                    && _context.AchTransactions.Any(transaction =>
                        transaction.AchCycleId == cycle.Id
                        && NachaExportEligibility.ExportableStates.Contains(transaction.State)),
                ExportUnavailableReason = _context.AchBatches.Any(batch => batch.AchCycleId == cycle.Id)
                    && _context.AchTransactions.Any(transaction =>
                        transaction.AchCycleId == cycle.Id
                        && NachaExportEligibility.ExportableStates.Contains(transaction.State))
                    ? null
                    : "El ciclo no tiene lotes y transacciones NACHA-M en estado exportable."
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

    private static AchCycleDto ApplyOperationalMetadata(AchCycleDto dto, AchCycle cycle)
    {
        var now = DateTime.UtcNow;
        var window = BuildCycleWindow(cycle.ProcessingDate, cycle.StartTime, cycle.EndTime);

        dto.WindowLabel = $"{window.Start:yyyy-MM-dd HH:mm} - {window.End:yyyy-MM-dd HH:mm}";
        dto.AcceptsTransactions = now >= window.Start && now <= window.End;
        dto.OperationalStatus = now < window.Start ? "Scheduled" : dto.AcceptsTransactions ? "Open" : "Closed";
        dto.IsContingencyCycle = IsContingencyCycle(cycle.CycleName);
        return dto;
    }

    private static (DateTime Start, DateTime End) BuildCycleWindow(DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            return (processingDate.Date + startTime, processingDate.Date + endTime);
        }

        return (processingDate.Date.AddDays(-1) + startTime, processingDate.Date + endTime);
    }

    private static bool IsContingencyCycle(string cycleName)
    {
        var normalized = (cycleName ?? string.Empty).Trim().ToUpperInvariant();
        return normalized.Contains("REPROCESO", StringComparison.Ordinal)
               || normalized.Contains("REPROC", StringComparison.Ordinal)
               || normalized.Contains("CONTING", StringComparison.Ordinal);
    }

    private async Task<int?> ResolveCycleConfigurationIdAsync(AchCycleRequest request, CancellationToken ct)
    {
        var processingDate = request.ProcessingDate.Date;
        return await _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(config => config.ClearingHouseId == request.ClearingHouseId
                && config.IsActive
                && config.CycleName == request.CycleName
                && config.EffectiveFrom <= processingDate
                && (!config.EffectiveTo.HasValue || config.EffectiveTo.Value >= processingDate))
            .OrderByDescending(config => config.EffectiveFrom)
            .ThenByDescending(config => config.Id)
            .Select(config => (int?)config.Id)
            .FirstOrDefaultAsync(ct);
    }
}
