using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Services;

public sealed class IntegrationRequestResolver : IIntegrationRequestResolver
{
    private readonly IIntegrationExecutionContractResolver _contractResolver;
    private readonly IIntegrationParameterValueResolver _valueResolver;

    public IntegrationRequestResolver(
        IIntegrationExecutionContractResolver contractResolver,
        IIntegrationParameterValueResolver valueResolver)
    {
        _contractResolver = contractResolver ?? throw new ArgumentNullException(nameof(contractResolver));
        _valueResolver = valueResolver ?? throw new ArgumentNullException(nameof(valueResolver));
    }

    public async Task<ResolvedIntegrationRequest> ResolveAsync(
        IntegrationExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var contract = await _contractResolver.ResolveActiveAsync(context.MethodCode, ct);
        var resolved = new List<ResolvedIntegrationParameter>();

        foreach (var binding in contract.Parameters
                     .Where(parameter => parameter.Direction == IntegrationParameterDirectionEnum.Input)
                     .OrderBy(parameter => parameter.SortOrder)
                     .ThenBy(parameter => parameter.ParameterPath, StringComparer.Ordinal))
        {
            if (!binding.IsMapped)
            {
                if (binding.Required || binding.MinOccurs > 0)
                {
                    throw new InvalidOperationException(
                        $"MAPPING_RULE_REQUIRED: {binding.ParameterPath} no tiene una regla habilitada en el mapping publicado.");
                }

                continue;
            }

            var value = await _valueResolver.ResolveAsync(binding, context, ct);
            resolved.Add(new ResolvedIntegrationParameter(
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
                binding.Direction));
        }

        return new ResolvedIntegrationRequest(contract, context.CorrelationId, resolved);
    }
}
