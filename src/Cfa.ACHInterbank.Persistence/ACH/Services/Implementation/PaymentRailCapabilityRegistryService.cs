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

    public PaymentRailCapabilityRegistryService(
        AchDbContext context,
        IPaymentRailContextService paymentRailContextService,
        IPaymentRailOperationalStrategyResolver strategyResolver)
    {
        _context = context;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
    }

    public async Task<IReadOnlyList<PaymentRailCapabilityRegistryItem>> GetEffectiveCapabilitiesAsync(
        int? clearingHouseId,
        string? clearingHouseCode,
        DateTime? asOfUtc = null,
        CancellationToken ct = default)
    {
        var now = (asOfUtc ?? DateTime.UtcNow).ToUniversalTime();
        var resolvedContext = _paymentRailContextService.ResolveContext(clearingHouseId, clearingHouseCode, null, now.Date);
        var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(clearingHouseId, clearingHouseCode, null));

        var overrides = await _context.Set<PaymentRailCapabilityRegistryEntry>()
            .AsNoTracking()
            .Where(x => x.RailCode == resolvedContext.RailCode
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
                    resolvedContext.RailCode,
                    capabilityCode,
                    ParseState(entry.State),
                    Source: "RegistryOverride",
                    Notes: entry.Notes,
                    EvaluatedAtUtc: evaluatedAt,
                    EffectiveFromUtc: entry.EffectiveFromUtc,
                    EffectiveToUtc: entry.EffectiveToUtc));
                continue;
            }

            result.Add(new PaymentRailCapabilityRegistryItem(
                resolvedContext.RailCode,
                capabilityCode,
                ResolveDefaultState(strategy, capabilityCode),
                Source: "StrategyDefault",
                Notes: "Estado derivado de strategy/wrapper pasivo; legacy owner preservado.",
                EvaluatedAtUtc: evaluatedAt,
                EffectiveFromUtc: null,
                EffectiveToUtc: null));
        }

        return result;
    }

    public async Task<PaymentRailCapabilityRegistryItem> UpsertCapabilityAsync(
        UpsertPaymentRailCapabilityRegistryRequest request,
        CancellationToken ct = default)
    {
        var normalizedRail = request.RailCode.Trim().ToUpperInvariant();
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
            entry.ChangeSource,
            entry.Notes,
            DateTime.UtcNow,
            entry.EffectiveFromUtc,
            entry.EffectiveToUtc);
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
