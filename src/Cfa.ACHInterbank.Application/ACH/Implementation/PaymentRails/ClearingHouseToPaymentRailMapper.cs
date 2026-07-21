using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class ClearingHouseToPaymentRailMapper : IClearingHouseToPaymentRailMapper
{
    // Estos aliases identifican estrategias regulatorias registradas; los IDs persistidos
    // nunca seleccionan comportamiento porque no son identidad funcional estable.
    private static readonly IReadOnlyDictionary<string, string> RailByClearingHouseCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ACH"] = PaymentRailCodes.AchColombia,
        ["ACHCOL"] = PaymentRailCodes.AchColombia,
        ["ACH COLOMBIA"] = PaymentRailCodes.AchColombia,
        ["CENIT"] = PaymentRailCodes.Cenit
    };

    public PaymentRailResolveResult ResolveRail(PaymentRailResolveRequest request)
    {
        var byRequestedRail = NormalizeRailCode(request.RequestedRailCode);
        if (byRequestedRail is not null)
        {
            return new PaymentRailResolveResult(byRequestedRail, true, "RequestedRailCode", "Riel resuelto por código solicitado.");
        }

        var normalizedCode = NormalizeCode(request.ClearingHouseCode);
        var hasCode = !string.IsNullOrWhiteSpace(normalizedCode);
        var byCode = hasCode && normalizedCode is not null && RailByClearingHouseCode.TryGetValue(normalizedCode, out var fromCode)
            ? fromCode
            : null;

        if (byCode is not null)
        {
            return new PaymentRailResolveResult(byCode, true, "ClearingHouseCode", "Riel resuelto por mapping explícito de ClearingHouseCode.");
        }

        return new PaymentRailResolveResult(PaymentRailCodes.Unknown, false, "Unknown", "No existe mapping explícito para la cámara indicada.");
    }

    private static string? NormalizeRailCode(string? railCode)
    {
        if (string.IsNullOrWhiteSpace(railCode))
        {
            return null;
        }

        var normalized = railCode.Trim().ToUpperInvariant();
        return normalized switch
        {
            PaymentRailCodes.AchColombia => PaymentRailCodes.AchColombia,
            PaymentRailCodes.Cenit => PaymentRailCodes.Cenit,
            _ => null
        };
    }

    private static string? NormalizeCode(string? clearingHouseCode)
    {
        return string.IsNullOrWhiteSpace(clearingHouseCode)
            ? null
            : clearingHouseCode.Trim().ToUpperInvariant();
    }
}
