using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Compatibility adapter for the current Proc_Contrapartidas typed request.
/// The common resolver remains authoritative for contract/mapping execution; this policy
/// only freezes the already-characterized legacy values/presence while the cutover flag is evaluated.
/// Unknown future contract inputs pass through unchanged so the generic writer is not coupled to this baseline.
/// </summary>
public sealed class ProcContrapartidasRequestParityPolicy
{
    private static readonly IReadOnlyDictionary<string, Func<ProcContrapartidasRequestContract, object?>> LegacyValueRegistry =
        new Dictionary<string, Func<ProcContrapartidasRequestContract, object?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["OFNIT"] = request => request.OFNIT,
            ["OFEMP"] = request => request.OFEMP,
            ["OFCTA"] = request => request.OFCTA,
            ["OFDD"] = request => request.OFDD,
            ["OFFECHEFEC"] = request => request.OFFECHEFEC,
            ["OFMONDEB"] = request => request.OFMONDEB,
            ["OFMONCRE"] = request => request.OFMONCRE,
            ["OFIDARCH"] = request => request.OFIDARCH,
            ["OFIDLOT"] = request => request.OFIDLOT,
            ["OFST"] = request => request.OFST,
            ["OFIDTX"] = request => request.OFIDTX,
            ["OFIDREVER"] = request => request.OFIDREVER,
            ["OFIDEBAPLI"] = request => request.OFIDEBAPLI,
            ["OFIDCAMCOMPE"] = request => request.OFIDCAMCOMPE,
            ["OFDIRECCIONIP"] = request => request.OFDIRECCIONIP,
            ["OFLIBRE"] = request => request.OFLIBRE,
            ["OFLIBRE1"] = request => request.OFLIBRE1,
            ["ANSIDLOTE"] = request => request.ANSIDLOTE,
            ["ANSST"] = request => request.ANSST,
            ["ANCLC"] = request => request.ANCLC,
            ["ANSIDTX"] = request => request.ANSIDTX,
            ["ANSIDREVER"] = request => request.ANSIDREVER
        };

    public ResolvedIntegrationRequest Apply(
        ResolvedIntegrationRequest request,
        ProcContrapartidasRequestContract legacyRequest)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(legacyRequest);

        var resolvedById = request.Parameters.ToDictionary(parameter => parameter.ParameterId);
        var normalized = new List<ResolvedIntegrationParameter>(request.Contract.Parameters.Count);

        foreach (var binding in request.Contract.Parameters
                     .Where(parameter => parameter.Direction == IntegrationParameterDirectionEnum.Input)
                     .OrderBy(parameter => parameter.SortOrder)
                     .ThenBy(parameter => parameter.ParameterPath, StringComparer.Ordinal))
        {
            if (!LegacyValueRegistry.TryGetValue(binding.ParameterPath, out var legacyValueResolver))
            {
                if (resolvedById.TryGetValue(binding.ParameterId, out var futureParameter))
                {
                    normalized.Add(futureParameter);
                }

                continue;
            }

            var legacyValue = legacyValueResolver(legacyRequest);
            if (resolvedById.TryGetValue(binding.ParameterId, out var resolved))
            {
                if (!string.Equals(
                        ToLegacyString(legacyValue),
                        ToLegacyString(resolved.Value),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"PROC_CONTRAPARTIDAS_PARITY_MISMATCH: {binding.ParameterPath} difiere del request tipado legacy.");
                }

                normalized.Add(CreateResolved(binding, resolved.Value));
                continue;
            }

            // The current typed baseline always writes the five optional ANS* placeholders.
            // Reintroduce only known legacy fields that belong to the active contract. New fields
            // remain governed exclusively by contract + mapping and therefore need no code change.
            normalized.Add(CreateResolved(binding, legacyValue));
        }

        return new ResolvedIntegrationRequest(request.Contract, request.CorrelationId, normalized);
    }

    private static ResolvedIntegrationParameter CreateResolved(
        IntegrationExecutionParameterBinding binding,
        object? value)
        => new(
            binding.ParameterId,
            binding.ParameterPath,
            binding.XmlElementName,
            binding.DataType,
            binding.XsdType,
            binding.Nillable,
            binding.MinOccurs,
            binding.MaxOccurs,
            binding.Required,
            binding.SortOrder,
            value,
            binding.Direction,
            isPresent: true);

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
}
