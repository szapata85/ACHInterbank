using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapRequestMapper : INachaSoapRequestMapper
{
    private readonly INachaSoapPayloadMapper? _payloadMapper;

    public NachaSoapRequestMapper(INachaSoapPayloadMapper? payloadMapper = null)
    {
        _payloadMapper = payloadMapper;
    }

    public NachaSoapMappedRequest Map(NachaSoapExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Decision);

        var decision = request.Decision;
        var errors = new List<string>();
        var isExecutable = ResolveExecutable(decision, errors);
        var requiresMonetaryMovement = ResolveMonetaryRequirement(decision);
        var methodName = ResolveMethodName(decision.SoapOperation);
        ValidateContext(request, errors);
        ValidateOperationDecisionCompatibility(decision, errors);
        var payloadMapping = request.PayloadContext is not null && _payloadMapper is not null
            ? _payloadMapper.Map(decision, request.PayloadContext)
            : null;

        if (decision.SoapOperation is NachaSoapOperationCandidate.ProcContrapartidas or NachaSoapOperationCandidate.ProcTransacciones
            && !decision.RequiresMonetaryMovement)
        {
            errors.Add($"{decision.SoapOperation} requiere movimiento monetario.");
            isExecutable = false;
        }

        if (payloadMapping is not null && !payloadMapping.IsExecutable)
        {
            errors.AddRange(payloadMapping.Errors);
            isExecutable = false;
        }

        if (decision.SoapOperation == NachaSoapOperationCandidate.RegistrarRespuestaTransaccion
            && decision.RequiresMonetaryMovement)
        {
            errors.Add("RegistrarRespuestaTransaccion no debe mover dinero.");
            isExecutable = false;
        }

        var trace = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Phase"] = "6B.5",
            ["CorrelationId"] = request.CorrelationId,
            ["Operation"] = decision.SoapOperation.ToString(),
            ["DecisionType"] = decision.DecisionType.ToString(),
            ["RequiresMonetaryMovement"] = requiresMonetaryMovement.ToString(),
            ["Executable"] = (isExecutable && errors.Count == 0).ToString(),
            ["ProductiveExecution"] = "false",
            ["NoGoReason"] = "Productivo permanece NO-GO; fase 6B.5.1 solo prepara/dry-run candidatos SOAP."
        };

        return new NachaSoapMappedRequest
        {
            Operation = decision.SoapOperation,
            DecisionType = decision.DecisionType,
            IsExecutable = isExecutable && errors.Count == 0,
            RequiresMonetaryMovement = requiresMonetaryMovement,
            WouldInvokeSoap = isExecutable && errors.Count == 0 && decision.SoapOperation != NachaSoapOperationCandidate.None,
            ProductiveExecution = false,
            MethodName = methodName,
            CorrelationId = request.CorrelationId,
            PayloadMapping = payloadMapping,
            Payload = BuildPayload(request, methodName, payloadMapping),
            Errors = errors,
            Trace = trace
        };
    }

    private static bool ResolveExecutable(NachaIncomingDecision decision, List<string> errors)
    {
        var executable = true;
        if (decision.SoapOperation == NachaSoapOperationCandidate.None)
        {
            errors.Add("Operacion None no es ejecutable.");
            executable = false;
        }

        if (decision.DecisionType is NachaIncomingDecisionType.ManualReviewRequired or NachaIncomingDecisionType.IgnoreDuplicate)
        {
            errors.Add($"{decision.DecisionType} no debe ejecutar SOAP.");
            executable = false;
        }

        return executable;
    }

    private static void ValidateContext(NachaSoapExecutionRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            errors.Add("CorrelationId es obligatorio para trazabilidad SOAP 6B.5.");
        }

        if (string.IsNullOrWhiteSpace(request.ClearingHouseCode))
        {
            errors.Add("ClearingHouseCode es obligatorio para frontera SOAP 6B.5.");
        }

        if (string.IsNullOrWhiteSpace(request.ProfileCode))
        {
            errors.Add("ProfileCode es obligatorio para frontera SOAP 6B.5.");
        }
    }

    private static void ValidateOperationDecisionCompatibility(NachaIncomingDecision decision, List<string> errors)
    {
        if (decision.SoapOperation == NachaSoapOperationCandidate.ProcTransacciones
            && decision.DecisionType != NachaIncomingDecisionType.ApplyCreditMovement)
        {
            errors.Add("ProcTransacciones solo es valido para ApplyCreditMovement.");
        }

        if (decision.SoapOperation == NachaSoapOperationCandidate.ProcContrapartidas
            && decision.DecisionType != NachaIncomingDecisionType.ApplyDebitMovement)
        {
            errors.Add("ProcContrapartidas solo es valido para ApplyDebitMovement.");
        }

        if (decision.SoapOperation == NachaSoapOperationCandidate.RegistrarRespuestaTransaccion
            && decision.DecisionType is not (NachaIncomingDecisionType.RegisterDifferentialResponse
                or NachaIncomingDecisionType.ApprovePrenotification
                or NachaIncomingDecisionType.RejectPrenotification
                or NachaIncomingDecisionType.MarkTransactionRejected
                or NachaIncomingDecisionType.MarkTransactionAccepted))
        {
            errors.Add("RegistrarRespuestaTransaccion solo es valido para respuestas diferenciales, prenotificaciones o actualizaciones de estado.");
        }
    }

    private static bool ResolveMonetaryRequirement(NachaIncomingDecision decision)
        => decision.SoapOperation switch
        {
            NachaSoapOperationCandidate.ProcContrapartidas => true,
            NachaSoapOperationCandidate.ProcTransacciones => true,
            NachaSoapOperationCandidate.RegistrarRespuestaTransaccion => false,
            _ => false
        };

    private static string ResolveMethodName(NachaSoapOperationCandidate operation)
        => operation switch
        {
            NachaSoapOperationCandidate.ProcContrapartidas => "Proc_Contrapartidas",
            NachaSoapOperationCandidate.ProcTransacciones => "Proc_Transacciones",
            NachaSoapOperationCandidate.RegistrarRespuestaTransaccion => "RegistrarRespuestaTransaccion",
            _ => string.Empty
        };

    private static Dictionary<string, string> BuildPayload(
        NachaSoapExecutionRequest request,
        string methodName,
        NachaSoapPayloadMappingResult? payloadMapping)
    {
        var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MethodName"] = methodName,
            ["CorrelationId"] = request.CorrelationId,
            ["ClearingHouseCode"] = request.ClearingHouseCode,
            ["ProfileCode"] = request.ProfileCode,
            ["ProductiveExecution"] = "false",
            ["RequestedBy"] = request.RequestedBy,
            ["Operation"] = request.Decision.SoapOperation.ToString(),
            ["DecisionType"] = request.Decision.DecisionType.ToString(),
            ["RequiresMonetaryMovement"] = request.Decision.RequiresMonetaryMovement.ToString(),
            ["EntryTraceNumber"] = request.Decision.EntryTraceNumber,
            ["OriginalTraceNumber"] = request.Decision.OriginalTraceNumber ?? string.Empty,
            ["TransactionId"] = request.Decision.TransactionId?.ToString() ?? string.Empty,
            ["PrenotificationId"] = request.Decision.PrenotificationId?.ToString() ?? string.Empty,
            ["ReasonCode"] = request.Decision.ReasonCode ?? string.Empty
        };

        if (payloadMapping is not null)
        {
            payload["PayloadType"] = payloadMapping.PayloadType;
            payload["PayloadIsMapped"] = payloadMapping.IsMapped.ToString();
            payload["PayloadSummaryPhase"] = payloadMapping.SanitizedSummary.TryGetValue("Phase", out var phase) ? phase : string.Empty;
            payload["PayloadSummaryOperation"] = payloadMapping.SanitizedSummary.TryGetValue("OperationCandidate", out var operation) ? operation : string.Empty;
        }

        foreach (var item in request.Metadata.Where(x => !IsSensitiveKey(x.Key)))
        {
            payload[$"Metadata.{item.Key}"] = MaskValue(item.Value);
        }

        return payload;
    }

    private static bool IsSensitiveKey(string key)
        => key.Contains("password", StringComparison.OrdinalIgnoreCase)
           || key.Contains("token", StringComparison.OrdinalIgnoreCase)
           || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
           || key.Contains("credential", StringComparison.OrdinalIgnoreCase);

    private static string MaskValue(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length >= 7 ? $"***{digits[^4..]}" : value ?? string.Empty;
    }
}
