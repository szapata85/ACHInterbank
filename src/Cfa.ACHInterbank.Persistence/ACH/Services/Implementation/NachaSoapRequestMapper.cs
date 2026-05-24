using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapRequestMapper : INachaSoapRequestMapper
{
    public NachaSoapMappedRequest Map(NachaSoapExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Decision);

        var decision = request.Decision;
        var errors = new List<string>();
        var isExecutable = ResolveExecutable(decision, errors);
        var requiresMonetaryMovement = ResolveMonetaryRequirement(decision);
        var methodName = ResolveMethodName(decision.SoapOperation);

        if (decision.SoapOperation is NachaSoapOperationCandidate.ProcContrapartidas or NachaSoapOperationCandidate.ProcTransacciones
            && !decision.RequiresMonetaryMovement)
        {
            errors.Add($"{decision.SoapOperation} requiere movimiento monetario.");
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
            MethodName = methodName,
            CorrelationId = request.CorrelationId,
            Payload = BuildPayload(request, methodName),
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

    private static Dictionary<string, string> BuildPayload(NachaSoapExecutionRequest request, string methodName)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["MethodName"] = methodName,
            ["CorrelationId"] = request.CorrelationId,
            ["ClearingHouseCode"] = request.ClearingHouseCode,
            ["ProfileCode"] = request.ProfileCode,
            ["EntryTraceNumber"] = request.Decision.EntryTraceNumber,
            ["OriginalTraceNumber"] = request.Decision.OriginalTraceNumber ?? string.Empty,
            ["TransactionId"] = request.Decision.TransactionId?.ToString() ?? string.Empty,
            ["PrenotificationId"] = request.Decision.PrenotificationId?.ToString() ?? string.Empty,
            ["ReasonCode"] = request.Decision.ReasonCode ?? string.Empty
        };
}
