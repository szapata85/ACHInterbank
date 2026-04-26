using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;

public interface IPaymentRailCapabilityRegistryService
{
    IReadOnlyList<PaymentRailRegistryRailItem> GetAvailableRails();

    Task<IReadOnlyList<PaymentRailCapabilityRegistryItem>> GetEffectiveCapabilitiesByRailAsync(
        string railCode,
        DateTime? asOfUtc = null,
        CancellationToken ct = default);

    Task<PaymentRailCapabilityRegistryItem?> GetEffectiveCapabilityByRailAsync(
        string railCode,
        string capabilityCode,
        DateTime? asOfUtc = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<PaymentRailCapabilityRegistryItem>> GetEffectiveCapabilitiesAsync(
        int? clearingHouseId,
        string? clearingHouseCode,
        DateTime? asOfUtc = null,
        CancellationToken ct = default);

    Task<PaymentRailCapabilityRegistryItem> UpsertCapabilityAsync(
        UpsertPaymentRailCapabilityRegistryRequest request,
        CancellationToken ct = default);
}
