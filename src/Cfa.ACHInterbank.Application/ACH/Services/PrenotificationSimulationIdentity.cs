using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Services;

/// <summary>
/// I03: la restriccion UNIQUE de NachaInboundSimulations.SimulationId funciona
/// como llave durable de idempotencia. La huella del payload evita reutilizar
/// una misma llave con una devolucion distinta. No incluye datos de cuenta.
/// </summary>
public static class PrenotificationSimulationIdentity
{
    public static Guid ReservationId(PrenotificationSimulationContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var key = request.IdempotencyKey?.Trim();
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("IDEMPOTENCY_KEY_REQUIRED");
        var normalized = $"PRENOTIFICATION_RETURN_V1|{request.ClearingHouseCode.Trim().ToUpperInvariant()}|{request.RespondingFinancialInstitutionId}|{key}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return new Guid(hash.AsSpan(0, 16));
    }

    public static string RequestHash(PrenotificationSimulationContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var canonical = JsonSerializer.Serialize(new
        {
            Outcome = request.Outcome.ToString(),
            ClearingHouse = request.ClearingHouseCode.Trim().ToUpperInvariant(),
            OriginalIds = request.OriginalPrenotificationIds.OrderBy(id => id).ToArray(),
            OriginalCycle = request.OriginalCycleId.Trim(),
            ResponseCycle = request.ResponseCycleId?.Trim(),
            request.BusinessDate,
            request.RespondingFinancialInstitutionId,
            ReasonCode = request.ReasonCode?.Trim().ToUpperInvariant()
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static string AuditMarker(string hash) => $"I03-PRENOTIFICATION-RETURN;REQUEST-SHA256={hash}";

    public static bool MatchesStoredPayload(string? notes, string expectedHash)
        => notes?.Contains(AuditMarker(expectedHash), StringComparison.Ordinal) == true;
}
