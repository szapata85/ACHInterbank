using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapUatReadinessChecker : INachaSoapUatReadinessChecker
{
    private readonly INachaSoapEndpointSafetyValidator _endpointSafetyValidator;
    private readonly INachaSoapCertificateReadinessValidator _certificateReadinessValidator;

    public NachaSoapUatReadinessChecker(
        INachaSoapEndpointSafetyValidator endpointSafetyValidator,
        INachaSoapCertificateReadinessValidator certificateReadinessValidator)
    {
        _endpointSafetyValidator = endpointSafetyValidator;
        _certificateReadinessValidator = certificateReadinessValidator;
    }

    public NachaSoapReadinessCheckResult CheckReadiness(
        string correlationId,
        NachaSoapUatControlOptions options,
        IReadOnlyList<NachaSoapEndpointDescriptor> endpoints)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(endpoints);

        var endpointChecks = endpoints.Select(x => _endpointSafetyValidator.Validate(x, options)).ToList();
        var certificateChecks = endpoints.Select(x => _certificateReadinessValidator.Validate(x, options)).ToList();
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!options.Enabled)
        {
            errors.Add("Control UAT deshabilitado.");
        }

        if (options.ProductiveExecution)
        {
            errors.Add("ProductiveExecution=true bloqueado. Productivo permanece NO-GO.");
        }

        if (options.AllowRealSoapInvocation)
        {
            errors.Add("AllowRealSoapInvocation=true bloqueado por NO-GO.");
        }

        if (options.AllowProductionEndpoints)
        {
            errors.Add("AllowProductionEndpoints=true bloqueado por NO-GO.");
        }

        if (options.AllowMonetaryOperations && options.ProductiveExecution)
        {
            errors.Add("Operaciones monetarias productivas bloqueadas por NO-GO.");
        }

        if (endpointChecks.Any(x => x.IsBlocked))
        {
            errors.Add("Uno o mas endpoints no son seguros para UAT.");
        }

        if (certificateChecks.Any(x => x.IsBlocked))
        {
            if (options.RequireCertificateValidation)
            {
                errors.Add("Readiness de certificado bloqueado por metadata incompleta.");
            }
            else
            {
                warnings.Add("Metadata de certificado incompleta; no bloquea porque RequireCertificateValidation=false.");
            }
        }
        else if (!options.RequireCertificateValidation
                 && certificateChecks.Any(x => !x.HasThumbprint || !x.HasStoreLocation))
        {
            warnings.Add("Metadata de certificado incompleta para UAT; no bloquea porque RequireCertificateValidation=false.");
        }

        var blocked = errors.Count > 0;
        return new NachaSoapReadinessCheckResult
        {
            CorrelationId = correlationId,
            EnvironmentName = options.EnvironmentName,
            IsReady = !blocked,
            IsBlocked = blocked,
            BlockReason = string.Join(" ", errors),
            ProductiveExecution = false,
            AllowRealSoapInvocation = options.AllowRealSoapInvocation,
            AllowMonetaryOperations = options.AllowMonetaryOperations,
            EndpointChecks = endpointChecks,
            CertificateChecks = certificateChecks,
            FeatureFlagChecks = BuildFeatureFlags(options),
            SecurityChecks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Productivo"] = "NO-GO",
                ["RealSoapInvocation"] = options.AllowRealSoapInvocation ? "Blocked" : "Disabled",
                ["RealCertificateStoreAccess"] = "false"
            },
            Warnings = warnings,
            Errors = errors,
            Metadata = NachaSoapEndpointSafetyValidator.SanitizeMetadata(options.Metadata)
        };
    }

    private static Dictionary<string, string> BuildFeatureFlags(NachaSoapUatControlOptions options)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["AchSoap:Enabled"] = options.Enabled.ToString(),
            ["AchSoap:Mode"] = options.Mode.ToString(),
            ["AchSoap:AllowRealInvocation"] = options.AllowRealSoapInvocation.ToString(),
            ["AchSoap:AllowProduction"] = options.AllowProductionEndpoints.ToString(),
            ["AchSoap:AllowMonetaryOperations"] = options.AllowMonetaryOperations.ToString(),
            ["AchSoap:RequireManualApproval"] = options.RequireManualApproval.ToString(),
            ["AchSoap:RequireCertificateValidation"] = options.RequireCertificateValidation.ToString(),
            ["AchSoap:BlockByNoGo"] = "true"
        };
}
