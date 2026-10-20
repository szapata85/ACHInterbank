using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

public sealed class IntegrationExecutionContractResolver : IIntegrationExecutionContractResolver
{
    private const string ConstantSourceKey = "constant.value";
    private readonly AchDbContext _context;

    public IntegrationExecutionContractResolver(AchDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IntegrationExecutionContract> ResolveActiveAsync(
        string methodCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(methodCode))
        {
            throw new ArgumentException("MethodCode es obligatorio.", nameof(methodCode));
        }

        var normalizedMethodCode = methodCode.Trim();
        var method = await _context.IntegrationMethods
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == normalizedMethodCode && x.IsActive, ct)
            ?? throw new InvalidOperationException(
                $"INTEGRATION_METHOD_NOT_FOUND: no existe IntegrationMethod activo para {normalizedMethodCode}.");

        var contract = await _context.IntegrationContractVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active,
                ct)
            ?? throw new InvalidOperationException(
                $"ACTIVE_CONTRACT_NOT_FOUND: no existe una versión contractual activa para {normalizedMethodCode}.");

        var mappingSets = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.ContractVersionId == contract.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);
        if (mappingSets.Count == 0)
        {
            throw new InvalidOperationException(
                $"PUBLISHED_MAPPING_NOT_FOUND: no existe IntegrationMappingSet publicado y activo para {normalizedMethodCode}.");
        }

        if (mappingSets.Count != 1)
        {
            throw new InvalidOperationException(
                $"MAPPING_SET_NOT_UNIQUE: existe más de un IntegrationMappingSet publicado y activo para {normalizedMethodCode}.");
        }

        var mappingSet = mappingSets[0];
        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.ContractVersionId == contract.Id
                && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var rules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Where(x => x.MappingSetId == mappingSet.Id && x.Enabled)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        if (rules.Count == 0)
        {
            throw new InvalidOperationException(
                $"El mapping set {mappingSet.Id} publicado no tiene reglas habilitadas.");
        }

        var firstRuleByParameter = rules
            .GroupBy(rule => rule.ParameterId)
            .ToDictionary(group => group.Key, group => group.First());

        var bindings = parameters.Select(parameter =>
        {
            if (!firstRuleByParameter.TryGetValue(parameter.Id, out var rule))
            {
                return new IntegrationExecutionParameterBinding(
                    parameter.Id,
                    parameter.ParameterPath,
                    parameter.XmlElementName,
                    parameter.DataType,
                    parameter.XsdType,
                    parameter.Nillable,
                    parameter.MinOccurs,
                    parameter.MaxOccurs,
                    parameter.Required,
                    parameter.SortOrder,
                    new IntegrationSourceReference(IntegrationSourceKindEnum.Constant, ConstantSourceKey),
                    direction: parameter.Direction,
                    isMapped: false);
            }

            if (!string.IsNullOrWhiteSpace(rule.ConditionExpression))
            {
                throw new NotSupportedException(
                    $"MAPPING_CONDITION_NOT_SUPPORTED: {parameter.ParameterPath} usa una expresión condicional arbitraria.");
            }

            var source = BuildSourceReference(rule);
            return new IntegrationExecutionParameterBinding(
                parameter.Id,
                parameter.ParameterPath,
                parameter.XmlElementName,
                parameter.DataType,
                parameter.XsdType,
                parameter.Nillable,
                parameter.MinOccurs,
                parameter.MaxOccurs,
                parameter.Required,
                parameter.SortOrder,
                source,
                rule.TransformationCode,
                rule.DefaultValue,
                rule.FormatMask,
                parameter.Direction,
                isMapped: true);
        }).ToArray();

        return new IntegrationExecutionContract(
            contract.Id,
            contract.Revision,
            method.Id,
            method.Code,
            contract.OperationName,
            contract.TargetNamespace,
            contract.SoapAction,
            mappingSet.Id,
            mappingSet.Version,
            mappingSet.Revision,
            bindings);
    }

    private static IntegrationSourceReference BuildSourceReference(IntegrationMappingRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return new IntegrationSourceReference(
                IntegrationSourceKindEnum.Constant,
                ConstantSourceKey,
                rule.FixedValue.Trim());
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Expression)
        {
            throw new NotSupportedException("El motor contract-driven no admite fuentes basadas en expresiones arbitrarias.");
        }

        if (string.IsNullOrWhiteSpace(rule.SourceFieldPath))
        {
            return new IntegrationSourceReference(
                IntegrationSourceKindEnum.Constant,
                ConstantSourceKey);
        }

        return new IntegrationSourceReference(
            rule.SourceKind,
            rule.SourceFieldPath);
    }
}
