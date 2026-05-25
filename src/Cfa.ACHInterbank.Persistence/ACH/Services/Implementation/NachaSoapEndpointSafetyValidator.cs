using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapEndpointSafetyValidator : INachaSoapEndpointSafetyValidator
{
    public NachaSoapEndpointCheckResult Validate(
        NachaSoapEndpointDescriptor endpoint,
        NachaSoapUatControlOptions options)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();
        if (options.RequireSafeEndpoint && string.IsNullOrWhiteSpace(endpoint.EndpointUrl))
        {
            errors.Add("Endpoint requerido no configurado.");
        }

        if (!endpoint.IsEnabled)
        {
            errors.Add("Endpoint deshabilitado.");
        }

        if (!endpoint.IsUat && !endpoint.IsProduction)
        {
            errors.Add("Ambiente de endpoint desconocido.");
        }

        if (endpoint.IsProduction || LooksProduction(endpoint.EndpointUrl))
        {
            errors.Add("Endpoint productivo bloqueado por NO-GO.");
        }

        if (endpoint.IsUat && !options.AllowUatEndpoints)
        {
            errors.Add("Endpoints UAT no permitidos por feature flag.");
        }

        if (options.AllowProductionEndpoints)
        {
            errors.Add("AllowProductionEndpoints=true bloqueado mientras Productivo permanece NO-GO.");
        }

        return new NachaSoapEndpointCheckResult
        {
            OperationCandidate = endpoint.OperationCandidate,
            EndpointName = endpoint.EndpointName,
            IsConfigured = !string.IsNullOrWhiteSpace(endpoint.EndpointUrl),
            IsEnabled = endpoint.IsEnabled,
            IsSafeForUat = errors.Count == 0 && endpoint.IsUat && !endpoint.IsProduction,
            IsProductionEndpoint = endpoint.IsProduction || LooksProduction(endpoint.EndpointUrl),
            IsBlocked = errors.Count > 0,
            BlockReason = string.Join(" ", errors),
            SanitizedEndpoint = SanitizeEndpoint(endpoint.EndpointUrl),
            Metadata = SanitizeMetadata(endpoint.Metadata)
        };
    }

    internal static string SanitizeEndpoint(string endpointUrl)
    {
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uri))
        {
            return "***invalid-endpoint***";
        }

        var host = uri.Host;
        var safeHost = host.Length <= 10 ? host : $"{host[..4]}***{host[^4..]}";
        return $"{uri.Scheme}://{safeHost}/***";
    }

    internal static Dictionary<string, string> SanitizeMetadata(IReadOnlyDictionary<string, string> metadata)
        => metadata
            .Where(x => !IsSensitiveKey(x.Key))
            .ToDictionary(x => x.Key, x => MaskValue(x.Value), StringComparer.OrdinalIgnoreCase);

    private static bool LooksProduction(string endpointUrl)
        => endpointUrl.Contains("prod", StringComparison.OrdinalIgnoreCase)
           || endpointUrl.Contains("production", StringComparison.OrdinalIgnoreCase)
           || endpointUrl.Contains("productivo", StringComparison.OrdinalIgnoreCase);

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
