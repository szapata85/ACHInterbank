using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;

public interface IPaymentRailCapabilityRegistryService
{
    Task<IReadOnlyList<PaymentRailCapabilityRegistryItem>> GetEffectiveCapabilitiesAsync(
        int? clearingHouseId,
        string? clearingHouseCode,
        DateTime? asOfUtc = null,
        CancellationToken ct = default);

    Task<PaymentRailCapabilityRegistryItem> UpsertCapabilityAsync(
        UpsertPaymentRailCapabilityRegistryRequest request,
        CancellationToken ct = default);
}
