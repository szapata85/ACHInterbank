using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class PaymentRailCapabilityRegistryService : IPaymentRailCapabilityRegistryService
{
    private readonly AchDbContext _context;
    private readonly IPaymentRailContextService _paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver _strategyResolver;

    private static readonly IReadOnlyDictionary<string, PaymentRailCapabilityKind> RegistryToWrapperCapability =
        new Dictionary<string, PaymentRailCapabilityKind>(StringComparer.OrdinalIgnoreCase)
        {
            [PaymentRailCapabilityRegistryCodes.CycleResolution] = PaymentRailCapabilityKind.Cycle,
            [PaymentRailCapabilityRegistryCodes.DispatchEligibility] = PaymentRailCapabilityKind.Dispatch,
            [PaymentRailCapabilityRegistryCodes.DispatchPlanning] = PaymentRailCapabilityKind.Dispatch,
            [PaymentRailCapabilityRegistryCodes.Returns] = PaymentRailCapabilityKind.Return,
            [PaymentRailCapabilityRegistryCodes.ReturnOfReturn] = PaymentRailCapabilityKind.Return,
            [PaymentRailCapabilityRegistryCodes.Netting] = PaymentRailCapabilityKind.Netting,
            [PaymentRailCapabilityRegistryCodes.Liquidity] = PaymentRailCapabilityKind.Liquidity
        };

    private static readonly IReadOnlyList<PaymentRailRegistryRailItem> AvailableRails =
    [
        new(PaymentRailCodes.AchColombia, "ACH Colombia", IsKnownRail: true, IsOperational: true, Source: "StrategyCatalog", Version: "prompt8.v1"),
        new(PaymentRailCodes.Cenit, "CENIT", IsKnownRail: true, IsOperational: true, Source: "StrategyCatalog", Version: "prompt8.v1"),
        new(PaymentRailCodes.Unknown, "UNKNOWN (fail-closed)", IsKnownRail: false, IsOperational: false, Source: "StrategyCatalog", Version: "prompt8.v1")
    ];

    public PaymentRailCapabilityRegistryService(
        AchDbContext context,
        IPaymentRailContextService paymentRailContextService,
        IPaymentRailOperationalStrategyResolver strategyResolver)
    {
        _context = context;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
    }

    public IReadOnlyList<PaymentRailRegistryRailItem> GetAvailableRails()
        => AvailableRails;

    public async Task<IReadOnlyList<PaymentRailCapabilityRegistryItem>> GetEffectiveCapabilitiesByRailAsync(
        string railCode,
        DateTime? asOfUtc = null,
        CancellationToken ct = default)
    {
        var normalizedRail = NormalizeRailCode(railCode);
        var now = (asOfUtc ?? DateTime.UtcNow).ToUniversalTime();
        var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(null, null, normalizedRail));

        return await BuildEffectiveCapabilitiesAsync(normalizedRail, strategy, now, ct);
    }

    public async Task<PaymentRailCapabilityRegistryItem?> GetEffectiveCapabilityByRailAsync(
        string railCode,
        string capabilityCode,
        DateTime? asOfUtc = null,
        CancellationToken ct = default)
    {
        var capabilities = await GetEffectiveCapabilitiesByRailAsync(railCode, asOfUtc, ct);
        return capabilities.FirstOrDefault(x => string.Equals(x.CapabilityCode, capabilityCode?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<PaymentRailCapabilityRegistryItem>> GetEffectiveCapabilitiesAsync(
        int? clearingHouseId,
        string? clearingHouseCode,
        DateTime? asOfUtc = null,
        CancellationToken ct = default)
    {
        var now = (asOfUtc ?? DateTime.UtcNow).ToUniversalTime();
        var normalizedClearingHouseCode = clearingHouseCode?.Trim().ToUpperInvariant();
        var paymentRailCode = await _context.ClearingHouses.AsNoTracking()
            .Where(x => (clearingHouseId.HasValue && x.Id == clearingHouseId.Value)
                        || (!string.IsNullOrWhiteSpace(normalizedClearingHouseCode) && x.Code == normalizedClearingHouseCode))
            .Select(x => x.ClearingHouseConfig.PaymentRailCode)
            .FirstOrDefaultAsync(ct);
        var resolvedContext = _paymentRailContextService.ResolveContext(clearingHouseId, clearingHouseCode, null, now.Date, paymentRailCode);
        var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(clearingHouseId, clearingHouseCode, paymentRailCode));

        return await BuildEffectiveCapabilitiesAsync(resolvedContext.RailCode, strategy, now, ct);
    }

    public async Task<PaymentRailCapabilityRegistryItem> UpsertCapabilityAsync(
        UpsertPaymentRailCapabilityRegistryRequest request,
        CancellationToken ct = default)
    {
        var normalizedRail = NormalizeRailCode(request.RailCode);
        var normalizedCapability = request.CapabilityCode.Trim();
        var effectiveFromUtc = (request.EffectiveFromUtc ?? DateTime.UtcNow).ToUniversalTime();

        var openEntries = await _context.Set<PaymentRailCapabilityRegistryEntry>()
            .Where(x => x.RailCode == normalizedRail
                        && x.CapabilityCode == normalizedCapability
                        && x.IsActive
                        && x.EffectiveToUtc == null)
            .ToListAsync(ct);

        foreach (var openEntry in openEntries)
        {
            openEntry.EffectiveToUtc = effectiveFromUtc;
            openEntry.IsActive = false;
            openEntry.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var entry = new PaymentRailCapabilityRegistryEntry
        {
            RailCode = normalizedRail,
            CapabilityCode = normalizedCapability,
            State = request.State.ToString(),
            EffectiveFromUtc = effectiveFromUtc,
            EffectiveToUtc = null,
            IsActive = true,
            ChangeSource = "Manual",
            ChangedBy = string.IsNullOrWhiteSpace(request.ChangedBy) ? "system" : request.ChangedBy.Trim(),
            ChangeTicket = request.ChangeTicket,
            Notes = request.Notes
        };

        _context.Set<PaymentRailCapabilityRegistryEntry>().Add(entry);
        await _context.SaveChangesAsync(ct);

        return new PaymentRailCapabilityRegistryItem(
            entry.RailCode,
            entry.CapabilityCode,
            request.State,
            PaymentRailCapabilityRegistrySources.RegistryOverride,
            entry.Notes,
            DateTime.UtcNow,
            entry.EffectiveFromUtc,
            entry.EffectiveToUtc,
            Version: $"registry:{entry.Id}",
            ChangeSource: entry.ChangeSource,
            ChangeTicket: entry.ChangeTicket,
            ChangedBy: entry.ChangedBy,
            ChangedAtUtc: entry.UpdatedAt);
    }

    private async Task<IReadOnlyList<PaymentRailCapabilityRegistryItem>> BuildEffectiveCapabilitiesAsync(
        string railCode,
        IPaymentRailOperationalStrategy strategy,
        DateTime now,
        CancellationToken ct)
    {
        var overrides = await _context.Set<PaymentRailCapabilityRegistryEntry>()
            .AsNoTracking()
            .Where(x => x.RailCode == railCode
                        && x.IsActive
                        && x.EffectiveFromUtc <= now
                        && (x.EffectiveToUtc == null || x.EffectiveToUtc >= now))
            .OrderByDescending(x => x.EffectiveFromUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        var overrideMap = overrides
            .GroupBy(x => x.CapabilityCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var evaluatedAt = DateTime.UtcNow;
        var result = new List<PaymentRailCapabilityRegistryItem>(PaymentRailCapabilityRegistryCodes.All.Count);

        foreach (var capabilityCode in PaymentRailCapabilityRegistryCodes.All)
        {
            if (overrideMap.TryGetValue(capabilityCode, out var entry))
            {
                result.Add(new PaymentRailCapabilityRegistryItem(
                    railCode,
                    capabilityCode,
                    ParseState(entry.State),
                    Source: PaymentRailCapabilityRegistrySources.RegistryOverride,
                    Notes: entry.Notes,
                    EvaluatedAtUtc: evaluatedAt,
                    EffectiveFromUtc: entry.EffectiveFromUtc,
                    EffectiveToUtc: entry.EffectiveToUtc,
                    Version: $"registry:{entry.Id}",
                    ChangeSource: entry.ChangeSource,
                    ChangeTicket: entry.ChangeTicket,
                    ChangedBy: entry.ChangedBy,
                    ChangedAtUtc: entry.UpdatedAt));
                continue;
            }

            result.Add(new PaymentRailCapabilityRegistryItem(
                railCode,
                capabilityCode,
                ResolveDefaultState(strategy, capabilityCode),
                Source: PaymentRailCapabilityRegistrySources.StrategyDefault,
                Notes: "Estado derivado de strategy/wrapper pasivo; legacy owner preservado.",
                EvaluatedAtUtc: evaluatedAt,
                EffectiveFromUtc: null,
                EffectiveToUtc: null,
                Version: $"strategy:{railCode}:v1"));
        }

        return result;
    }

    private static string NormalizeRailCode(string railCode)
    {
        if (string.IsNullOrWhiteSpace(railCode))
        {
            throw new ArgumentException("RailCode requerido.", nameof(railCode));
        }

        var normalized = railCode.Trim().ToUpperInvariant();
        return normalized switch
        {
            PaymentRailCodes.AchColombia => PaymentRailCodes.AchColombia,
            PaymentRailCodes.Cenit => PaymentRailCodes.Cenit,
            PaymentRailCodes.Unknown => PaymentRailCodes.Unknown,
            _ => throw new ArgumentException($"RailCode no soportado: {railCode}", nameof(railCode))
        };
    }

    private static PaymentRailCapabilityRegistryState ResolveDefaultState(IPaymentRailOperationalStrategy strategy, string capabilityCode)
    {
        if (!RegistryToWrapperCapability.TryGetValue(capabilityCode, out var capabilityKind))
        {
            return PaymentRailCapabilityRegistryState.Planned;
        }

        var status = strategy.CapabilityStatuses.FirstOrDefault(x => x.Capability == capabilityKind);
        if (status is null)
        {
            return PaymentRailCapabilityRegistryState.NotSupported;
        }

        return status.ExecutionMode switch
        {
            PaymentRailCapabilityExecutionMode.NotSupported => PaymentRailCapabilityRegistryState.NotSupported,
            PaymentRailCapabilityExecutionMode.WrapperPassive => PaymentRailCapabilityRegistryState.ShadowOnly,
            PaymentRailCapabilityExecutionMode.LegacyOwner => PaymentRailCapabilityRegistryState.Enabled,
            PaymentRailCapabilityExecutionMode.StrategyOwnerPlanned => PaymentRailCapabilityRegistryState.Planned,
            _ => PaymentRailCapabilityRegistryState.Planned
        };
    }

    private static PaymentRailCapabilityRegistryState ParseState(string state)
        => Enum.TryParse<PaymentRailCapabilityRegistryState>(state, ignoreCase: true, out var parsed)
            ? parsed
            : PaymentRailCapabilityRegistryState.Planned;
}
