using System.Globalization;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class ProcTransaccionesRequestParityPolicy
{
    public ProcTransaccionesRequestParityResult Apply(ResolvedIntegrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bindings = request.Contract.Parameters.ToDictionary(binding => binding.ParameterId);
        var legacyParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sourceValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normalizedParameters = new List<ResolvedIntegrationParameter>(request.Parameters.Count);

        foreach (var parameter in request.Parameters
                     .OrderBy(item => item.SortOrder)
                     .ThenBy(item => item.ParameterPath, StringComparer.Ordinal))
        {
            var binding = bindings[parameter.ParameterId];
            var value = ToLegacyString(parameter.Value);
            var placeholder = !binding.Required && IsPlaceholder(value);
            if (binding.Required && string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"El parámetro requerido {binding.ParameterPath} no pudo resolverse.");
            }

            if (!placeholder)
            {
                legacyParameters[binding.ParameterPath] = value;
                if (binding.IsMapped
                    && binding.Source.SourceKind != IntegrationSourceKindEnum.Constant
                    && !string.IsNullOrWhiteSpace(binding.Source.SourceKey))
                {
                    sourceValues[binding.Source.SourceKey] = value;
                }
            }

            var serialize = !placeholder
                && !string.IsNullOrWhiteSpace(value)
                && !string.Equals(binding.ParameterPath, "METODO", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(binding.ParameterPath, "ILR", StringComparison.OrdinalIgnoreCase);
            normalizedParameters.Add(new ResolvedIntegrationParameter(
                parameter.ParameterId,
                parameter.ParameterPath,
                parameter.XmlElementName,
                parameter.DataType,
                parameter.XsdType,
                parameter.Nillable,
                parameter.MinOccurs,
                parameter.MaxOccurs,
                parameter.Required,
                parameter.SortOrder,
                parameter.Value,
                parameter.Direction,
                isPresent: serialize));
        }

        return new ProcTransaccionesRequestParityResult(
            new ResolvedIntegrationRequest(request.Contract, request.CorrelationId, normalizedParameters),
            legacyParameters,
            sourceValues);
    }

    private static string ToLegacyString(object? value)
        => value switch
        {
            null => string.Empty,
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

    private static bool IsPlaceholder(string? value)
        => string.Equals(value?.Trim(), "SEED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value?.Trim(), "PLACEHOLDER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value?.Trim(), "TODO", StringComparison.OrdinalIgnoreCase);
}

public sealed record ProcTransaccionesRequestParityResult(
    ResolvedIntegrationRequest ContractDrivenRequest,
    IReadOnlyDictionary<string, string> Parameters,
    IReadOnlyDictionary<string, string> SourceValues);
