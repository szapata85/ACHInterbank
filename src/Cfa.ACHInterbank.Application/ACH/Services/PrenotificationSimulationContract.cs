using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

/// <summary>
/// Contrato I01 de forma (NO validacion de negocio). I02 verificara en BD la
/// correlacion, plazo, camara, banco, estado y la idempotencia antes de ejecutar.
/// </summary>
public static class PrenotificationSimulationContract
{
    public const string Version = "I01";

    public static PrenotificationSimulationContractCapabilitiesResponse Describe()
        => new(
            Version,
            nameof(NachaInboundSimulationType.IncomingPrenotificationReturn),
            nameof(NachaInboundSimulationType.IncomingPrenotificationResponse),
            ["ACHCOL", "CENIT"],
            true, false, false,
            [
                new(PrenotificationSimulationOutcome.Returned, true, true, false,
                    "Devolucion NACHA-M con causal, generada y descargada en UAT; posterior NachaUpload manual (I03/I04)."),
                new(PrenotificationSimulationOutcome.NoReturnByDeadline, false, false, false,
                    "No existe archivo de aprobacion; verificar vencimiento y job de aceptacion tacita aislado (I05).")
            ]);

    public static PrenotificationSimulationContractValidationResponse Validate(PrenotificationSimulationContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<PrenotificationSimulationContractViolation>();
        void Add(string code, string field, string message) => errors.Add(new(code, field, message));

        if (!Enum.IsDefined(request.Outcome))
            Add("PRENOTIFICATION_OUTCOME_INVALID", nameof(request.Outcome), "Resultado desconocido.");
        if (string.IsNullOrWhiteSpace(request.ClearingHouseCode)
            || !(request.ClearingHouseCode.Equals("ACHCOL", StringComparison.OrdinalIgnoreCase)
                 || request.ClearingHouseCode.Equals("CENIT", StringComparison.OrdinalIgnoreCase)))
            Add("CLEARING_HOUSE_UNSUPPORTED", nameof(request.ClearingHouseCode), "Solo ACHCOL o CENIT.");
        if (request.OriginalPrenotificationIds is null || request.OriginalPrenotificationIds.Count == 0
            || request.OriginalPrenotificationIds.Any(id => id <= 0))
            Add("PRENOTIFICATION_IDS_REQUIRED", nameof(request.OriginalPrenotificationIds), "Seleccionar IDs originales positivos.");
        else if (request.OriginalPrenotificationIds.Count > 100)
            Add("PRENOTIFICATION_IDS_LIMIT_EXCEEDED", nameof(request.OriginalPrenotificationIds), "Maximo 100 IDs por consulta.");
        else if (request.OriginalPrenotificationIds.Distinct().Count() != request.OriginalPrenotificationIds.Count)
            Add("PRENOTIFICATION_IDS_DUPLICATED", nameof(request.OriginalPrenotificationIds), "No repetir prenotificaciones.");
        if (string.IsNullOrWhiteSpace(request.OriginalCycleId))
            Add("ORIGINAL_CYCLE_REQUIRED", nameof(request.OriginalCycleId), "Indicar ciclo de origen.");
        if (request.BusinessDate == default)
            Add("BUSINESS_DATE_REQUIRED", nameof(request.BusinessDate), "Indicar fecha operacional.");
        if (request.RespondingFinancialInstitutionId <= 0)
            Add("RESPONDING_FINANCIAL_INSTITUTION_REQUIRED", nameof(request.RespondingFinancialInstitutionId), "Indicar banco receptor original.");

        if (request.Outcome == PrenotificationSimulationOutcome.Returned)
        {
            if (string.IsNullOrWhiteSpace(request.ResponseCycleId))
                Add("RESPONSE_CYCLE_REQUIRED", nameof(request.ResponseCycleId), "Una devolucion requiere ciclo de respuesta.");
            if (string.IsNullOrWhiteSpace(request.ReasonCode))
                Add("RETURN_REASON_REQUIRED", nameof(request.ReasonCode), "Una devolucion requiere causal oficial.");
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 128)
                Add("IDEMPOTENCY_KEY_REQUIRED", nameof(request.IdempotencyKey), "Clave de idempotencia requerida (maximo 128 caracteres).");
        }
        else if (request.Outcome == PrenotificationSimulationOutcome.NoReturnByDeadline)
        {
            if (!string.IsNullOrWhiteSpace(request.ResponseCycleId))
                Add("NO_RETURN_HAS_NO_RESPONSE_CYCLE", nameof(request.ResponseCycleId), "No hay ciclo de respuesta si no hay devolucion.");
            if (!string.IsNullOrWhiteSpace(request.ReasonCode))
                Add("NO_RETURN_HAS_NO_REASON", nameof(request.ReasonCode), "Sin devolucion no corresponde causal.");
        }

        return new(errors.Count == 0, false, Version, errors);
    }
}
