using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;

/// <summary>
/// Freezes the characterized RegistrarRespuestaTransaccion v1 wire contract during the
/// feature-flagged migration to the common contract-driven request engine.
/// </summary>
internal sealed class RegistrarRespuestaRequestParityPolicy
{
    internal const string MethodCode = "WSAXON.RegistrarRespuestaTransaccion";
    private static readonly Guid BaselineContractVersionId = new("4a093eb5-beed-5b15-bc7c-5768c0e0547e");
    private const string OperationName = "RegistrarRespuestaTransaccion";
    private const string TargetNamespace = "http://tempuri.org/";
    private const string SoapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion";

    private static readonly IReadOnlyList<BaselineField> BaselineFields =
    [
        new("idCanal", "int", true, command => command.IdCanal),
        new("nombreCanal", "string", true, command => command.NombreCanal),
        new("idTransaccion", "string", true, command => command.IdTransaccion),
        new("idEstado", "int", true, command => command.IdEstado),
        new("causal", "string", false, command => command.Causal),
        new("idTransaccionAxon", "int", true, command => command.IdTransaccionServicioExterno),
        new("descripcionCausal", "string", false, command => command.DescripcionCausal)
    ];

    public ResolvedIntegrationRequest EnsureBaselineV1(
        ResolvedIntegrationRequest request,
        RegistrarRespuestaAchCommand command)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(command);

        if (request.ContractVersionId != BaselineContractVersionId
            || !string.Equals(request.MethodCode, MethodCode, StringComparison.Ordinal)
            || !string.Equals(request.OperationName, OperationName, StringComparison.Ordinal)
            || !string.Equals(request.TargetNamespace, TargetNamespace, StringComparison.Ordinal)
            || !string.Equals(request.SoapAction, SoapAction, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "REGISTRAR_RESPUESTA_CONTRACT_V1_REQUIRED: el cutover contract-driven exige el contrato SOAP baseline v1 caracterizado.");
        }

        var inputBindings = request.Contract.Parameters
            .Where(parameter => parameter.Direction == IntegrationParameterDirectionEnum.Input)
            .OrderBy(parameter => parameter.SortOrder)
            .ThenBy(parameter => parameter.ParameterPath, StringComparer.Ordinal)
            .ToArray();

        if (request.Contract.Parameters.Count != BaselineFields.Count
            || inputBindings.Length != BaselineFields.Count
            || request.Parameters.Count != BaselineFields.Count)
        {
            throw new InvalidOperationException(
                "REGISTRAR_RESPUESTA_BASELINE_FIELDS_MISMATCH: RegistrarRespuestaTransaccion v1 exige exactamente siete campos de entrada.");
        }

        var resolvedById = request.Parameters.ToDictionary(parameter => parameter.ParameterId);
        for (var index = 0; index < BaselineFields.Count; index++)
        {
            var expected = BaselineFields[index];
            var binding = inputBindings[index];

            if (!string.Equals(binding.ParameterPath, expected.Name, StringComparison.Ordinal)
                || !string.Equals(binding.XmlElementName, expected.Name, StringComparison.Ordinal)
                || !string.Equals(NormalizeType(binding.DataType), expected.DataType, StringComparison.Ordinal)
                || binding.Required != expected.Required
                || binding.SortOrder != index + 1
                || binding.MaxOccurs != 1)
            {
                throw new InvalidOperationException(
                    $"REGISTRAR_RESPUESTA_BASELINE_FIELD_INVALID: el campo {expected.Name} no coincide con el baseline v1.");
            }

            if (!resolvedById.TryGetValue(binding.ParameterId, out var resolved) || !resolved.IsPresent)
            {
                throw new InvalidOperationException(
                    $"REGISTRAR_RESPUESTA_BASELINE_FIELD_MISSING: el campo {expected.Name} debe estar presente en v1.");
            }

            if (!string.Equals(
                    ToLegacyString(expected.Value(command)),
                    ToLegacyString(resolved.Value),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"REGISTRAR_RESPUESTA_PARITY_MISMATCH: {expected.Name} difiere del command validado por el flujo legacy.");
            }
        }

        return request;
    }

    private static string NormalizeType(string value)
    {
        var normalized = value.Trim();
        var separator = normalized.IndexOf(':');
        if (separator >= 0 && separator < normalized.Length - 1)
        {
            normalized = normalized[(separator + 1)..];
        }

        return normalized.ToLowerInvariant();
    }

    private static string ToLegacyString(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return text;
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private sealed record BaselineField(
        string Name,
        string DataType,
        bool Required,
        Func<RegistrarRespuestaAchCommand, object?> Value);
}
