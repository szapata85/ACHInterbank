using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
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
    private readonly IAchCycleScheduler _cycleScheduler;
    private readonly IPaymentRailContextService _paymentRailContextService;
    private readonly ILogger<RoutingStrategyService> _logger;
    private readonly IOperationalCycleWindowResolver _windowResolver;
    private readonly IOperationalCalendarService _calendar;

    public RoutingStrategyService(
        AchDbContext context,
        IBankHoliday holidayService,
        IAchCycleScheduler cycleScheduler,
        IPaymentRailContextService? paymentRailContextService = null,
        ILogger<RoutingStrategyService>? logger = null,
        IOperationalCycleWindowResolver? windowResolver = null,
        IOperationalCalendarService? calendar = null)
    {
        _context = context;
        _ = holidayService;
        _cycleScheduler = cycleScheduler;
        _paymentRailContextService = paymentRailContextService ?? new NullPaymentRailContextService();
        _logger = logger ?? NullLogger<RoutingStrategyService>.Instance;
        _windowResolver = windowResolver ?? new OperationalCycleWindowResolver();
        _calendar = calendar ?? new OperationalCalendarService(context);
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
        var clearingHouseById = await _context.ClearingHouses
            .AsNoTracking()
            .Where(ch => prefIds.Contains(ch.Id))
            .Select(ch => new
            {
                ch.Id,
                ch.Code,
                TimeZoneId = ch.ClearingHouseConfig.TimeZoneId
            })
            .ToDictionaryAsync(ch => ch.Id, ct);

        // 2️) Próxima fecha hábil
        var firstPreference = preferences[0];
        var firstHouse = clearingHouseById[firstPreference.ClearingHouseId];
        var firstTimeZoneId = ResolveTimeZoneId(firstHouse.Code, firstHouse.TimeZoneId);
        var currentInstant = ResolveCurrentInstant(now, firstTimeZoneId);
        var firstLocalNow = _windowResolver.Resolve(
            now.Date,
            TimeSpan.Zero,
            new TimeSpan(23, 59, 59),
            firstTimeZoneId,
            currentInstant).LocalNow;
        DateTime processingDate = firstLocalNow.Date;

        // 3️) Buscar el siguiente ciclo disponible, avanzando al próximo día hábil
        //     cuando los cortes de hoy ya pasaron.
        for (int attempt = 0; attempt < 15; attempt++)
        {
            // 4️) Evaluar cámaras en el orden IsDefault + Priority
            foreach (InstitutionClearingHousePreference pref in preferences)
            {
                var candidateDate = await _calendar.GetNextBusinessDayAsync(
                    DateOnly.FromDateTime(processingDate),
                    pref.ClearingHouseId,
                    ct);
                var candidateProcessingDate = candidateDate.ToDateTime(TimeOnly.MinValue);
                var hasCycles = await _context.AchCycles.AnyAsync(c =>
                    c.ClearingHouseId == pref.ClearingHouseId
                    && c.ProcessingDate.Date == candidateProcessingDate.Date,
                    ct);
                if (!hasCycles)
                {
                    await _cycleScheduler.ScheduleCyclesForClearingHouseAsync(
                        pref.ClearingHouseId,
                        candidateProcessingDate);
                }

                var candidateCyclesRaw = await _context.AchCycles
                    .Where(c =>
                        c.ClearingHouseId == pref.ClearingHouseId &&
                        c.ProcessingDate.Date == candidateProcessingDate.Date)
                    .OrderBy(c => c.ProcessingDate)
                    .ToListAsync(ct);

                var candidateCycles = candidateCyclesRaw
                    .OrderBy(c => c.ProcessingDate)
                    .ThenBy(c => c.CutoffTime)
                    .ToList();

                var house = clearingHouseById[pref.ClearingHouseId];
                var timeZoneId = ResolveTimeZoneId(house.Code, house.TimeZoneId);
                var candidateInstant = ResolveCurrentInstant(now, timeZoneId);
                var nextCycle = candidateCycles
                    .Select(c => new
                    {
                        Cycle = c,
                        Window = _windowResolver.Resolve(c.ProcessingDate, c.StartTime, c.CutoffTime, timeZoneId, candidateInstant)
                    })
                    .Where(x => x.Window.Status != OperationalCycleWindowStatus.After)
                    .OrderBy(x => x.Window.EndInstant)
                    .ThenBy(x => x.Cycle.CutoffTime)
                    .Select(x => x.Cycle)
                    .FirstOrDefault();

                if (nextCycle != null)
                {
                    var clearingHouseCode = house.Code;
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
            processingDate = processingDate.AddDays(1);
        }

        throw new InvalidOperationException(
            "No hay ciclos disponibles para las cámaras habilitadas en el orden de IsDefault y prioridad.");
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

    private DateTimeOffset ResolveCurrentInstant(DateTime suppliedTime, string timeZoneId)
    {
        if (suppliedTime.Kind == DateTimeKind.Utc)
        {
            return new DateTimeOffset(suppliedTime, TimeSpan.Zero);
        }

        return _windowResolver.ConvertLocalToInstant(
            DateTime.SpecifyKind(suppliedTime, DateTimeKind.Unspecified),
            timeZoneId);
    }

    private static string ResolveTimeZoneId(string clearingHouseCode, string? configuredTimeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(configuredTimeZoneId))
        {
            return configuredTimeZoneId;
        }

        if (string.Equals(clearingHouseCode, RegulatoryCycleScheduleCatalog.AchColombiaCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearingHouseCode, RegulatoryCycleScheduleCatalog.CenitCode, StringComparison.OrdinalIgnoreCase))
        {
            return RegulatoryCycleScheduleCatalog.BogotaTimeZoneId;
        }

        throw new InvalidOperationException($"La cámara '{clearingHouseCode}' no tiene zona horaria operativa configurada.");
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
