using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class IntegrationMappingReadinessService : IIntegrationMappingReadinessService
{
    private readonly AchDbContext _context;
    private readonly IIntegrationCatalogService _catalogService;

    public IntegrationMappingReadinessService(AchDbContext context, IIntegrationCatalogService catalogService)
    {
        _context = context;
        _catalogService = catalogService;
    }

    public Task<IntegrationMappingReadinessResult> EvaluateAsync(
        TransactionIntegrationOperationResult operation,
        CancellationToken ct = default)
        => operation.IsSupported
            ? EvaluateAsync(
                operation.IntegrationKey,
                operation.OperationKey,
                operation.MappingPurpose,
                operation.MappingDirection,
                operation.TransactionId,
                null,
                ct)
            : Task.FromResult(Failed(
                operation.IntegrationKey,
                operation.OperationKey,
                operation.MappingPurpose,
                operation.MappingDirection,
                "INTEGRATION_OPERATION_NOT_SUPPORTED",
                operation.Errors.Count > 0 ? operation.Errors : ["La operacion de integracion no esta soportada."]));

    public async Task<IntegrationMappingReadinessResult> EvaluateAsync(
        string integrationKey,
        string operationKey,
        string mappingPurpose,
        string mappingDirection,
        int? transactionId = null,
        object? sourcePayload = null,
        CancellationToken ct = default)
    {
        integrationKey = (integrationKey ?? string.Empty).Trim();
        operationKey = (operationKey ?? string.Empty).Trim();
        mappingPurpose = (mappingPurpose ?? string.Empty).Trim();
        mappingDirection = (mappingDirection ?? string.Empty).Trim();

        await _catalogService.GetMethodsAsync(ct);

        var methodCode = $"{integrationKey}.{operationKey}";
        var method = await _context.IntegrationMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == methodCode && x.IsActive, ct);

        if (method is null)
        {
            return Failed(
                integrationKey,
                operationKey,
                mappingPurpose,
                mappingDirection,
                "INTEGRATION_METHOD_NOT_CONFIGURED",
                [$"No existe IntegrationMethod activo para {methodCode}."]);
        }

        var published = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        if (published is null)
        {
            return SupportsTransitionalFallback(operationKey, mappingPurpose)
                ? PartialFallback(
                    integrationKey,
                    operationKey,
                    mappingPurpose,
                    mappingDirection,
                    "No existe IntegrationMappingSet publicado; solo queda disponible fallback transicional trazado.")
                : Failed(
                    integrationKey,
                    operationKey,
                    mappingPurpose,
                    mappingDirection,
                    "INTEGRATION_MAPPING_REQUIRED",
                    [$"No existe IntegrationMappingSet publicado para {methodCode}."]);
        }

        var requiredParameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.IsActive
                && x.Required
                && x.Direction == IntegrationParameterDirectionEnum.Input)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ParameterPath)
            .ToListAsync(ct);

        var requiredIds = requiredParameters.Select(x => x.Id).ToHashSet();
        var rules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Where(x => x.MappingSetId == published.Id && requiredIds.Contains(x.ParameterId))
            .ToListAsync(ct);

        var ruleGroups = rules.GroupBy(x => x.ParameterId).ToDictionary(x => x.Key, x => x.ToList());
        var missing = new List<string>();
        var inactive = new List<string>();
        var activeMappings = 0;

        foreach (var parameter in requiredParameters)
        {
            if (!ruleGroups.TryGetValue(parameter.Id, out var parameterRules) || parameterRules.Count == 0)
            {
                missing.Add(parameter.ParameterPath);
                continue;
            }

            if (parameterRules.Any(x => x.Enabled))
            {
                activeMappings++;
            }
            else
            {
                inactive.Add(parameter.ParameterPath);
            }
        }

        if (missing.Count > 0 || inactive.Count > 0)
        {
            return new IntegrationMappingReadinessResult(
                IsReady: false,
                Status: "Failed",
                Code: "INTEGRATION_MAPPING_REQUIRED",
                IntegrationKey: integrationKey,
                OperationKey: operationKey,
                MappingPurpose: mappingPurpose,
                MappingDirection: mappingDirection,
                RequiredMappings: requiredParameters.Count,
                ActiveMappings: activeMappings,
                MissingRequiredMappings: missing,
                InactiveRequiredMappings: inactive,
                UsesFallback: false,
                CanBuildPayload: false,
                Errors: ["Faltan mappings requeridos activos para construir payload SOAP/XML."],
                Warnings: []);
        }

        return new IntegrationMappingReadinessResult(
            IsReady: true,
            Status: "Ok",
            Code: "OK",
            IntegrationKey: integrationKey,
            OperationKey: operationKey,
            MappingPurpose: mappingPurpose,
            MappingDirection: mappingDirection,
            RequiredMappings: requiredParameters.Count,
            ActiveMappings: activeMappings,
            MissingRequiredMappings: [],
            InactiveRequiredMappings: [],
            UsesFallback: false,
            CanBuildPayload: true,
            Errors: [],
            Warnings: []);
    }

    private static bool SupportsTransitionalFallback(string operationKey, string mappingPurpose)
        => operationKey == IntegrationGuaranteeConstants.ProcContrapartidas
            && mappingPurpose == IntegrationGuaranteeConstants.MonetaryDebitRequest;

    private static IntegrationMappingReadinessResult PartialFallback(
        string integrationKey,
        string operationKey,
        string mappingPurpose,
        string mappingDirection,
        string warning)
        => new(
            IsReady: false,
            Status: "Partial",
            Code: "INTEGRATION_MAPPING_FALLBACK",
            IntegrationKey: integrationKey,
            OperationKey: operationKey,
            MappingPurpose: mappingPurpose,
            MappingDirection: mappingDirection,
            RequiredMappings: 0,
            ActiveMappings: 0,
            MissingRequiredMappings: [],
            InactiveRequiredMappings: [],
            UsesFallback: true,
            CanBuildPayload: true,
            Errors: [],
            Warnings: [warning]);

    private static IntegrationMappingReadinessResult Failed(
        string integrationKey,
        string operationKey,
        string mappingPurpose,
        string mappingDirection,
        string code,
        IReadOnlyCollection<string> errors)
        => new(
            IsReady: false,
            Status: "Failed",
            Code: code,
            IntegrationKey: integrationKey,
            OperationKey: operationKey,
            MappingPurpose: mappingPurpose,
            MappingDirection: mappingDirection,
            RequiredMappings: 0,
            ActiveMappings: 0,
            MissingRequiredMappings: [],
            InactiveRequiredMappings: [],
            UsesFallback: false,
            CanBuildPayload: false,
            Errors: errors,
            Warnings: []);
}
