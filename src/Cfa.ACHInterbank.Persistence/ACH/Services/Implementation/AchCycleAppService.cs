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
        var configuration = await ResolveCycleConfigurationAsync(request, null, ct);

        var entity = _mapper.Map<AchCycle>(request);
        ApplyCanonicalConfiguration(entity, configuration, request.ProcessingDate);
        entity.Id = AchCycleIdHelper.GenerateId(request.ClearingHouseId, configuration.CycleName, request.ProcessingDate.Date);

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
        var configuration = await ResolveCycleConfigurationAsync(request, entity.ClearingHouseCycleConfigId, ct);
        var configurationChanged = entity.ClearingHouseCycleConfigId != configuration.Id;
        if (configurationChanged && await _context.AchTransactions.AnyAsync(x => x.AchCycleId == entity.Id, ct))
        {
            throw new InvalidOperationException(
                "No es posible cambiar la configuración canónica de un ciclo que ya tiene transacciones.");
        }

        _mapper.Map(request, entity);
        ApplyCanonicalConfiguration(entity, configuration, request.ProcessingDate);

        await _context.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task<AchCycleConfigurationLinkRepairResult> RepairConfigurationLinksAsync(CancellationToken ct = default)
    {
        var cycles = await _context.AchCycles
            .Where(x => x.ClearingHouseCycleConfigId == null)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        if (cycles.Count == 0)
        {
            return new AchCycleConfigurationLinkRepairResult(true, 0, 0, 0, 0, []);
        }

        var houseIds = cycles.Select(x => x.ClearingHouseId).Distinct().ToArray();
        var configurations = await _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(x => houseIds.Contains(x.ClearingHouseId))
            .ToListAsync(ct);

        var decisions = cycles
            .Select(cycle => ResolveHistoricalConfiguration(cycle, configurations))
            .ToList();
        var ambiguous = decisions.Where(x => x.Status == HistoricalLinkStatus.Ambiguous).ToList();
        var unmatched = decisions.Where(x => x.Status == HistoricalLinkStatus.Unmatched).ToList();

        if (ambiguous.Count > 0)
        {
            return BuildRepairResult(false, cycles.Count, [], ambiguous, unmatched);
        }

        var repairable = decisions.Where(x => x.Status == HistoricalLinkStatus.Unique).ToList();
        foreach (var decision in repairable)
        {
            decision.Cycle.ClearingHouseCycleConfigId = decision.ConfigurationId;
        }

        if (repairable.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return BuildRepairResult(true, cycles.Count, repairable, [], unmatched);
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

    private async Task<ClearingHouseCycleConfig> ResolveCycleConfigurationAsync(
        AchCycleRequest request,
        int? existingConfigurationId,
        CancellationToken ct)
    {
        if (request.ProcessingDate == default)
        {
            throw new InvalidOperationException("La fecha operativa del ciclo es requerida.");
        }

        var processingDate = request.ProcessingDate.Date;
        var requestedId = request.ClearingHouseCycleConfigId ?? existingConfigurationId;
        var effectiveConfigurations = await _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(config => config.ClearingHouseId == request.ClearingHouseId
                && config.IsActive
                && config.EffectiveFrom <= processingDate
                && (!config.EffectiveTo.HasValue || config.EffectiveTo.Value >= processingDate))
            .ToListAsync(ct);

        if (requestedId.HasValue)
        {
            var selected = effectiveConfigurations.SingleOrDefault(x => x.Id == requestedId.Value)
                ?? throw new InvalidOperationException(
                    "La configuración seleccionada no existe, está inactiva, no está vigente o pertenece a otra cámara.");
            EnsureConfigurationIsNotAmbiguous(selected, effectiveConfigurations);
            return selected;
        }

        var windowMatches = effectiveConfigurations
            .Where(x => WindowMatches(x, request.StartTime, request.EndTime, request.CutoffTime))
            .ToList();
        var resolved = ResolveUniqueCandidate(windowMatches, request.CycleName);
        return resolved ?? throw new InvalidOperationException(windowMatches.Count == 0
            ? "No existe una configuración activa y vigente que corresponda al ciclo solicitado. Seleccione una configuración de ciclo."
            : "La configuración del ciclo es ambigua. Seleccione explícitamente una configuración canónica.");
    }

    private static void EnsureConfigurationIsNotAmbiguous(
        ClearingHouseCycleConfig selected,
        IReadOnlyCollection<ClearingHouseCycleConfig> effectiveConfigurations)
    {
        var normalizedName = NormalizeCycleName(selected.CycleName);
        if (effectiveConfigurations.Count(x => NormalizeCycleName(x.CycleName) == normalizedName) > 1)
        {
            throw new InvalidOperationException(
                "Existen configuraciones activas superpuestas para el ciclo seleccionado. Corrija la ambigüedad antes de crear la instancia operativa.");
        }
    }

    private static ClearingHouseCycleConfig? ResolveUniqueCandidate(
        IReadOnlyCollection<ClearingHouseCycleConfig> windowMatches,
        string? cycleName)
    {
        if (windowMatches.Count == 1)
        {
            return windowMatches.Single();
        }

        if (windowMatches.Count == 0)
        {
            return null;
        }

        var normalizedName = NormalizeCycleName(cycleName);
        var nameMatches = windowMatches
            .Where(x => NormalizeCycleName(x.CycleName) == normalizedName)
            .ToList();
        return nameMatches.Count == 1 ? nameMatches[0] : null;
    }

    private static HistoricalLinkDecision ResolveHistoricalConfiguration(
        AchCycle cycle,
        IReadOnlyCollection<ClearingHouseCycleConfig> configurations)
    {
        var date = cycle.ProcessingDate.Date;
        var windowMatches = configurations
            .Where(config => config.ClearingHouseId == cycle.ClearingHouseId
                && config.EffectiveFrom.Date <= date
                && (!config.EffectiveTo.HasValue || config.EffectiveTo.Value.Date >= date)
                && WindowMatches(config, cycle.StartTime, cycle.EndTime, cycle.CutoffTime))
            .ToList();
        var resolved = ResolveUniqueCandidate(windowMatches, cycle.CycleName);
        if (resolved is not null)
        {
            return new HistoricalLinkDecision(cycle, resolved.Id, HistoricalLinkStatus.Unique,
                "Coincidencia única por cámara, vigencia y ventana operativa.");
        }

        return windowMatches.Count == 0
            ? new HistoricalLinkDecision(cycle, null, HistoricalLinkStatus.Unmatched,
                "No existe una configuración históricamente aplicable con la misma ventana operativa.")
            : new HistoricalLinkDecision(cycle, null, HistoricalLinkStatus.Ambiguous,
                "Existe más de una configuración históricamente aplicable para la misma ventana operativa.");
    }

    private static AchCycleConfigurationLinkRepairResult BuildRepairResult(
        bool completed,
        int inspected,
        IReadOnlyCollection<HistoricalLinkDecision> repaired,
        IReadOnlyCollection<HistoricalLinkDecision> ambiguous,
        IReadOnlyCollection<HistoricalLinkDecision> unmatched)
    {
        var items = repaired.Concat(ambiguous).Concat(unmatched)
            .OrderBy(x => x.Cycle.Id)
            .Select(x => new AchCycleConfigurationLinkRepairItem(
                x.Cycle.Id,
                x.ConfigurationId,
                x.Status.ToString(),
                x.Detail))
            .ToList();
        return new AchCycleConfigurationLinkRepairResult(
            completed, inspected, repaired.Count, ambiguous.Count, unmatched.Count, items);
    }

    private static void ApplyCanonicalConfiguration(
        AchCycle cycle,
        ClearingHouseCycleConfig configuration,
        DateTime processingDate)
    {
        cycle.ProcessingDate = processingDate.Date;
        cycle.ClearingHouseCycleConfigId = configuration.Id;
        cycle.CycleName = configuration.CycleName.Trim();
        cycle.StartTime = configuration.StartTime;
        cycle.EndTime = configuration.EndTime;
        cycle.CutoffTime = configuration.CutoffTime;
    }

    private static bool WindowMatches(
        ClearingHouseCycleConfig configuration,
        TimeSpan startTime,
        TimeSpan endTime,
        TimeSpan cutoffTime)
        => configuration.StartTime == startTime
            && configuration.EndTime == endTime
            && configuration.CutoffTime == cutoffTime;

    private static string NormalizeCycleName(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();

    private enum HistoricalLinkStatus { Unique, Ambiguous, Unmatched }

    private sealed record HistoricalLinkDecision(
        AchCycle Cycle,
        int? ConfigurationId,
        HistoricalLinkStatus Status,
        string Detail);
}
