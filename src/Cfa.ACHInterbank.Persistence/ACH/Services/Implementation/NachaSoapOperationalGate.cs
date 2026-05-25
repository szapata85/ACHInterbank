using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapOperationalGate : INachaSoapOperationalGate
{
    private readonly INachaSoapEndpointSafetyValidator _endpointSafetyValidator;

    public NachaSoapOperationalGate(INachaSoapEndpointSafetyValidator endpointSafetyValidator)
    {
        _endpointSafetyValidator = endpointSafetyValidator;
    }

    public NachaSoapOperationalGateResult Evaluate(
        NachaSoapExecutionRequest request,
        NachaSoapUatControlOptions options,
        IReadOnlyList<NachaSoapEndpointDescriptor> endpoints)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(endpoints);

        var reasons = new List<string>();
        var operation = request.Decision.SoapOperation;
        var endpoint = endpoints.FirstOrDefault(x => x.OperationCandidate == operation);
        var endpointCheck = endpoint is null
            ? null
            : _endpointSafetyValidator.Validate(endpoint, options);

        if (!options.Enabled || options.Mode == NachaSoapOperationalMode.Disabled)
        {
            reasons.Add("AchSoap deshabilitado.");
        }

        if (options.ProductiveExecution)
        {
            reasons.Add("ProductiveExecution=true bloqueado. Productivo permanece NO-GO.");
        }

        if (options.AllowRealSoapInvocation || options.Mode == NachaSoapOperationalMode.UatControlled)
        {
            reasons.Add("Invocacion SOAP real bloqueada por NO-GO en Fase 6B.5.5.");
        }

        if (options.AllowProductionEndpoints || endpointCheck?.IsProductionEndpoint == true)
        {
            reasons.Add("Endpoint productivo bloqueado.");
        }

        if (request.Decision.RequiresMonetaryMovement && options.ProductiveExecution)
        {
            reasons.Add("Movimiento monetario productivo bloqueado.");
        }

        if (options.RequireManualApproval && !options.ManualApprovalGranted)
        {
            reasons.Add("Aprobacion manual requerida no otorgada.");
        }

        if (endpointCheck?.IsBlocked == true)
        {
            reasons.Add(endpointCheck.BlockReason);
        }

        var modeAllowed = options.Mode is NachaSoapOperationalMode.DryRun
            or NachaSoapOperationalMode.Simulated
            or NachaSoapOperationalMode.UatReadiness;
        var blocked = reasons.Count > 0 || !modeAllowed;

        return new NachaSoapOperationalGateResult
        {
            CorrelationId = request.CorrelationId,
            OperationCandidate = operation,
            Mode = options.Mode,
            IsAllowed = !blocked,
            IsBlocked = blocked,
            BlockReason = string.Join(" ", reasons),
            ProductiveExecution = false,
            WouldInvokeRealSoap = false,
            Audit = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Phase"] = "6B.5",
                ["Productivo"] = "NO-GO",
                ["Mode"] = options.Mode.ToString(),
                ["OperationCandidate"] = operation.ToString(),
                ["Endpoint"] = endpointCheck?.SanitizedEndpoint ?? string.Empty,
                ["BlockReason"] = string.Join(" ", reasons),
                ["WouldInvokeRealSoap"] = "false"
            }
        };
    }
}
