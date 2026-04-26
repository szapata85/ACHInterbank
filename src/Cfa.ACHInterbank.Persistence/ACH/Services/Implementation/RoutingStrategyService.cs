using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class RoutingStrategyService : IRoutingStrategyService
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;
    private readonly IAchCycleScheduler _cycleScheduler;
    private readonly IPaymentRailContextService _paymentRailContextService;
    private readonly ILogger<RoutingStrategyService> _logger;

    public RoutingStrategyService(
        AchDbContext context,
        IBankHoliday holidayService,
        IAchCycleScheduler cycleScheduler,
        IPaymentRailContextService? paymentRailContextService = null,
        ILogger<RoutingStrategyService>? logger = null)
    {
        _context = context;
        _holidayService = holidayService;
        _cycleScheduler = cycleScheduler;
        _paymentRailContextService = paymentRailContextService ?? new NullPaymentRailContextService();
        _logger = logger ?? NullLogger<RoutingStrategyService>.Instance;
    }

    public async Task<string> ResolveClearingHouseForTransactionAsync(
    int destinationInstitutionId,
    DateTime now,
    CancellationToken ct)
    {
        // 1️) Cargar institución y preferencias
        FinancialInstitution? fi = await _context.FinancialInstitutions
            .Include(f => f.ClearingHousePreferences)
            .FirstOrDefaultAsync(f => f.Id == destinationInstitutionId, ct)
            ?? throw new InvalidOperationException("Institución destino no encontrada.");

        // Primero los que son default, luego por prioridad
        List<InstitutionClearingHousePreference> allPreferences = fi.ClearingHousePreferences
            .OrderByDescending(p => p.IsDefault)  // true primero
            .ThenBy(p => NormalizePriority(p.Priority))
            .ThenBy(p => p.Id)
            .ToList();

        if (!allPreferences.Any())
        {
            throw new InvalidOperationException("La institución no tiene cámaras asociadas.");
        }

        List<InstitutionClearingHousePreference> preferences = allPreferences
            .Where(p => p.IsActive)
            .ToList();

        if (!preferences.Any())
        {
            throw new InvalidOperationException("La institución destino no tiene cámaras activas configuradas.");
        }

        var prefIds = preferences.Select(p => p.ClearingHouseId).Distinct().ToList();
        var clearingHouseCodeById = await _context.ClearingHouses
            .AsNoTracking()
            .Where(ch => prefIds.Contains(ch.Id))
            .ToDictionaryAsync(ch => ch.Id, ch => ch.Code, ct);

        // 2️) Próxima fecha hábil
        DateTime processingDate = GetNextBusinessDay(now.Date);

        // 3️) Buscar el siguiente ciclo disponible, avanzando al próximo día hábil
        //     cuando los cortes de hoy ya pasaron.
        for (int attempt = 0; attempt < 15; attempt++)
        {
            // Crear ciclos on-demand solo si faltan
            List<int> existing = await _context.AchCycles
                .Where(c => prefIds.Contains(c.ClearingHouseId) &&
                            c.ProcessingDate.Date == processingDate.Date)
                .Select(c => c.ClearingHouseId)
                .Distinct()
                .ToListAsync(ct);

            List<int> missing = prefIds.Except(existing).ToList();
            foreach (int chId in missing)
            {
                await _cycleScheduler.ScheduleCyclesForClearingHouseAsync(chId, processingDate);
            }

            // 4️) Evaluar cámaras en el orden IsDefault + Priority
            foreach (InstitutionClearingHousePreference pref in preferences)
            {
                var candidateCyclesRaw = await _context.AchCycles
                    .Where(c =>
                        c.ClearingHouseId == pref.ClearingHouseId &&
                        c.ProcessingDate.Date == processingDate.Date)
                    .OrderBy(c => c.ProcessingDate)
                    .ToListAsync(ct);

                var candidateCycles = candidateCyclesRaw
                    .OrderBy(c => c.ProcessingDate)
                    .ThenBy(c => c.CutoffTime)
                    .ToList();

                var nextCycle = candidateCycles
                    .Select(c => new { Cycle = c, Window = BuildCycleWindow(c.ProcessingDate, c.StartTime, c.EndTime) })
                    .Where(x => now <= x.Window.End)
                    .OrderBy(x => x.Window.End)
                    .ThenBy(x => x.Cycle.CutoffTime)
                    .Select(x => x.Cycle)
                    .FirstOrDefault();

                if (nextCycle != null)
                {
                    clearingHouseCodeById.TryGetValue(nextCycle.ClearingHouseId, out var clearingHouseCode);
                    var resolvedContext = _paymentRailContextService.ResolveContext(
                        nextCycle.ClearingHouseId,
                        clearingHouseCode,
                        nextCycle.Id,
                        nextCycle.ProcessingDate);
                    var shadowSnapshot = _paymentRailContextService.BuildShadowSnapshot(
                        resolvedContext,
                        legacySource: "ClearingHouseId",
                        legacyValue: nextCycle.ClearingHouseId.ToString());

                    _logger.LogInformation(
                        "PAYMENT_RAIL_RESOLVED|CorrelationId={CorrelationId}|LegacySource={LegacySource}|LegacyValue={LegacyValue}|RailCode={RailCode}|IsKnownRail={IsKnownRail}|StrategyRailCode={StrategyRailCode}|ResolutionSource={ResolutionSource}",
                        shadowSnapshot.CorrelationId,
                        shadowSnapshot.LegacySource,
                        shadowSnapshot.LegacyValue,
                        shadowSnapshot.RailCode,
                        shadowSnapshot.IsKnownRail,
                        shadowSnapshot.StrategyRailCode,
                        resolvedContext.ResolutionSource);

                    return nextCycle.Id;
                }
            }

            // Si no hay ciclos con cortes futuros en la fecha actual,
            // avanzamos al siguiente día hábil y volvemos a intentar.
            processingDate = GetNextBusinessDay(processingDate.AddDays(1));
        }

        throw new InvalidOperationException(
            "No hay ciclos disponibles para las cámaras habilitadas en el orden de IsDefault y prioridad.");
    }


    private DateTime GetNextBusinessDay(DateTime startDate)
    {
        var date = startDate;
        var holidays = _holidayService.GetHolidays(date.Year)
                                      .Select(h => h.Date)
                                      .ToHashSet();

        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
               holidays.Contains(DateOnly.FromDateTime(date)))
        {
            date = date.AddDays(1);

            if (!holidays.Any(h => h.Year == date.Year))
            {
                holidays = _holidayService.GetHolidays(date.Year)
                                          .Select(h => h.Date)
                                          .ToHashSet();
            }
        }
        return date;
    }

    private static int NormalizePriority(int priority)
    {
        if (priority <= 1)
        {
            return 1;
        }

        if (priority >= 3)
        {
            return 3;
        }

        return 2;
    }

    private static (DateTime Start, DateTime End) BuildCycleWindow(DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            return (processingDate.Date + startTime, processingDate.Date + endTime);
        }

        return (processingDate.Date.AddDays(-1) + startTime, processingDate.Date + endTime);
    }

    private sealed class NullPaymentRailContextService : IPaymentRailContextService
    {
        public PaymentRailResolvedContext ResolveContext(
            int? clearingHouseId,
            string? clearingHouseCode,
            string? achCycleId,
            DateTime? operationalDate,
            string? requestedRailCode = null,
            string? correlationId = null)
        {
            var ctx = new PaymentRailOperationalContext(
                clearingHouseId,
                clearingHouseCode,
                achCycleId,
                operationalDate,
                string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId);

            return new PaymentRailResolvedContext(
                RailCode: PaymentRailCodes.Unknown,
                IsKnownRail: false,
                ResolutionSource: "NullBridge",
                ResolutionMessage: "PaymentRailContextService no registrado; bridge en modo pasivo.",
                StrategyRailCode: PaymentRailCodes.Unknown,
                Capabilities: new PaymentRailCapabilityDescriptor(false, false, false, false, false, false, "Null bridge"),
                CapabilityStatuses:
                [
                    new(PaymentRailCapabilityKind.Cycle, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Null bridge"),
                    new(PaymentRailCapabilityKind.Dispatch, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Null bridge"),
                    new(PaymentRailCapabilityKind.Return, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Null bridge"),
                    new(PaymentRailCapabilityKind.Netting, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Null bridge"),
                    new(PaymentRailCapabilityKind.Liquidity, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Null bridge")
                ],
                OperationalContext: ctx);
        }

        public PaymentRailShadowCompareSnapshot BuildShadowSnapshot(
            PaymentRailResolvedContext resolvedContext,
            string legacySource,
            string legacyValue)
        {
            return new PaymentRailShadowCompareSnapshot(
                legacySource,
                legacyValue,
                resolvedContext.RailCode,
                resolvedContext.IsKnownRail,
                resolvedContext.StrategyRailCode,
                resolvedContext.OperationalContext.CorrelationId,
                DateTime.UtcNow);
        }
    }
}
