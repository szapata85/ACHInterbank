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
    private const string OkCode = "OK";
    private const string ReadyWithWarningsCode = "READY_WITH_WARNINGS";
    private const string FunctionalPlaceholderCode = "FUNCTIONAL_MAPPING_PLACEHOLDER";
    private const string RegistrarContractInvalidCode = "REGISTRAR_WSDL_CONTRACT_INVALID";

    private static readonly string[] RegistrarRespuestaWsdlParameterPaths =
    [
        "idCanal",
        "nombreCanal",
        "idTransaccion",
        "idEstado",
        "causal",
        "idTransaccionAxon",
        "descripcionCausal"
    ];

    private static readonly string[] RegistrarRespuestaNonWsdlParameterPaths =
    [
        "ANSIDLOTE",
        "ANSST",
        "ANCLC",
        "ANSIDTX",
        "ANSIDREVER"
    ];

    private static readonly HashSet<string> PlaceholderValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "SEED",
        "TEST",
        "REF-1",
        "ACH",
        "000010070",
        "900123456",
        "0",
        "0.0",
        "0.00",
        "0.0.0.0",
        "1",
        "1.0",
        "1.00",
        "constant.value"
    };

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
        await new IntegrationMappingBootstrapper(_context).EnsureAsync(ct);

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

        var activeInputParameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.IsActive
                && x.Direction == IntegrationParameterDirectionEnum.Input)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ParameterPath)
            .ToListAsync(ct);

        var registrarContractErrors = ValidateRegistrarRespuestaContract(operationKey, activeInputParameters);
        if (registrarContractErrors.Count > 0)
        {
            return new IntegrationMappingReadinessResult(
                IsReady: false,
                Status: "Failed",
                Code: RegistrarContractInvalidCode,
                IntegrationKey: integrationKey,
                OperationKey: operationKey,
                MappingPurpose: mappingPurpose,
                MappingDirection: mappingDirection,
                RequiredMappings: 0,
                ActiveMappings: 0,
                MissingRequiredMappings: [],
                InactiveRequiredMappings: [],
                FallbackFields: [],
                RequiredFallbackFields: [],
                UsesFallback: false,
                CanBuildPayload: false,
                Errors: registrarContractErrors,
                Warnings: []);
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
            var requiredFallbackFields = activeInputParameters
                .Where(x => x.Required)
                .Select(x => x.ParameterPath)
                .ToList();

            return new IntegrationMappingReadinessResult(
                IsReady: false,
                Status: "Failed",
                Code: SupportsTransitionalFallback(operationKey, mappingPurpose)
                    ? "REQUIRED_MAPPING_USES_FALLBACK"
                    : "INTEGRATION_MAPPING_REQUIRED",
                IntegrationKey: integrationKey,
                OperationKey: operationKey,
                MappingPurpose: mappingPurpose,
                MappingDirection: mappingDirection,
                RequiredMappings: requiredFallbackFields.Count,
                ActiveMappings: 0,
                MissingRequiredMappings: requiredFallbackFields,
                InactiveRequiredMappings: [],
                FallbackFields: SupportsTransitionalFallback(operationKey, mappingPurpose) ? requiredFallbackFields : [],
                RequiredFallbackFields: SupportsTransitionalFallback(operationKey, mappingPurpose) ? requiredFallbackFields : [],
                UsesFallback: SupportsTransitionalFallback(operationKey, mappingPurpose),
                CanBuildPayload: false,
                Errors: [$"No existe IntegrationMappingSet publicado para {methodCode}; no se permite fallback para campos requeridos."],
                Warnings: []);
        }

        var requiredParameters = activeInputParameters
            .Where(x => x.Required)
            .ToList();

        var requiredIds = requiredParameters.Select(x => x.Id).ToHashSet();
        var rules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Where(x => x.MappingSetId == published.Id && requiredIds.Contains(x.ParameterId))
            .ToListAsync(ct);

        var ruleGroups = rules.GroupBy(x => x.ParameterId).ToDictionary(x => x.Key, x => x.ToList());
        var missing = new List<string>();
        var inactive = new List<string>();
        var activeMappings = 0;
        var functionalErrors = new List<string>();
        var functionalWarnings = new List<string>();

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
                var winner = parameterRules
                    .Where(x => x.Enabled)
                    .OrderBy(x => x.Priority)
                    .ThenBy(x => x.Id)
                    .First();
                var assessment = AssessFunctionalCoverage(operationKey, parameter, winner);
                functionalErrors.AddRange(assessment.Errors);
                functionalWarnings.AddRange(assessment.Warnings);
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
                FallbackFields: [],
                RequiredFallbackFields: [],
                UsesFallback: false,
                CanBuildPayload: false,
                Errors: ["Faltan mappings requeridos activos para construir payload SOAP/XML."],
                Warnings: []);
        }

        if (functionalErrors.Count > 0)
        {
            return new IntegrationMappingReadinessResult(
                IsReady: false,
                Status: "Failed",
                Code: FunctionalPlaceholderCode,
                IntegrationKey: integrationKey,
                OperationKey: operationKey,
                MappingPurpose: mappingPurpose,
                MappingDirection: mappingDirection,
                RequiredMappings: requiredParameters.Count,
                ActiveMappings: activeMappings,
                MissingRequiredMappings: [],
                InactiveRequiredMappings: [],
                FallbackFields: [],
                RequiredFallbackFields: [],
                UsesFallback: false,
                CanBuildPayload: false,
                Errors: functionalErrors,
                Warnings: functionalWarnings);
        }

        if (functionalWarnings.Count > 0)
        {
            return new IntegrationMappingReadinessResult(
                IsReady: true,
                Status: "ReadyWithWarnings",
                Code: ReadyWithWarningsCode,
                IntegrationKey: integrationKey,
                OperationKey: operationKey,
                MappingPurpose: mappingPurpose,
                MappingDirection: mappingDirection,
                RequiredMappings: requiredParameters.Count,
                ActiveMappings: activeMappings,
                MissingRequiredMappings: [],
                InactiveRequiredMappings: [],
                FallbackFields: [],
                RequiredFallbackFields: [],
                UsesFallback: false,
                CanBuildPayload: true,
                Errors: [],
                Warnings: functionalWarnings);
        }

        return new IntegrationMappingReadinessResult(
            IsReady: true,
            Status: "Ok",
            Code: OkCode,
            IntegrationKey: integrationKey,
            OperationKey: operationKey,
            MappingPurpose: mappingPurpose,
            MappingDirection: mappingDirection,
            RequiredMappings: requiredParameters.Count,
            ActiveMappings: activeMappings,
            MissingRequiredMappings: [],
            InactiveRequiredMappings: [],
            FallbackFields: [],
            RequiredFallbackFields: [],
            UsesFallback: false,
            CanBuildPayload: true,
            Errors: [],
            Warnings: []);
    }

    private static bool SupportsTransitionalFallback(string operationKey, string mappingPurpose)
        => operationKey == IntegrationGuaranteeConstants.ProcContrapartidas
            && mappingPurpose == IntegrationGuaranteeConstants.MonetaryDebitRequest;

    private static IReadOnlyCollection<string> ValidateRegistrarRespuestaContract(
        string operationKey,
        IReadOnlyCollection<IntegrationMethodParameter> activeInputParameters)
    {
        if (!string.Equals(operationKey, IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var activePaths = activeInputParameters
            .Select(x => x.ParameterPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedPaths = RegistrarRespuestaWsdlParameterPaths
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nonWsdlPaths = activePaths
            .Where(x => RegistrarRespuestaNonWsdlParameterPaths.Contains(x, StringComparer.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToList();

        var errors = new List<string>();
        if (nonWsdlPaths.Count > 0)
        {
            errors.Add($"RegistrarRespuestaTransaccion contiene parametros no-WSDL activos: {string.Join(", ", nonWsdlPaths)}.");
        }

        if (!activePaths.SetEquals(expectedPaths))
        {
            var missing = expectedPaths.Except(activePaths, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            var unexpected = activePaths.Except(expectedPaths, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            errors.Add($"RegistrarRespuestaTransaccion debe exponer exactamente los 7 parametros WSDL. Faltantes: {FormatList(missing)}. Sobrantes: {FormatList(unexpected)}.");
        }

        return errors;
    }

    private static FunctionalCoverageAssessment AssessFunctionalCoverage(
        string operationKey,
        IntegrationMethodParameter parameter,
        IntegrationMappingRule rule)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var parameterPath = parameter.ParameterPath;
        var fixedValue = NormalizeValue(rule.FixedValue);
        var defaultValue = NormalizeValue(rule.DefaultValue);
        var sourcePath = NormalizeValue(rule.SourceFieldPath);

        if (rule.SourceKind == IntegrationSourceKindEnum.Constant)
        {
            var value = fixedValue ?? defaultValue ?? sourcePath;
            if (IsNonBlockingTechnicalConstant(operationKey, parameterPath, value))
            {
                warnings.Add(WarningMessage(operationKey, parameterPath, value, "constante tecnica documentada; validar contra politica funcional antes de salida productiva"));
                return new FunctionalCoverageAssessment([], warnings);
            }

            if (IsReservedResponsePlaceholder(operationKey, parameterPath))
            {
                warnings.Add(WarningMessage(operationKey, parameterPath, value, "campo contractual de respuesta/reservado; no se decide direccion en esta fase"));
                return new FunctionalCoverageAssessment([], warnings);
            }

            if (IsAllowedFunctionalConstant(operationKey, parameterPath, value))
            {
                return new FunctionalCoverageAssessment([], []);
            }

            errors.Add(ErrorMessage(operationKey, parameterPath, value, "parametro requerido cubierto por constante sin politica funcional homologada"));
            return new FunctionalCoverageAssessment(errors, warnings);
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Expression)
        {
            warnings.Add(WarningMessage(operationKey, parameterPath, sourcePath ?? rule.ConditionExpression, "expresion requiere validacion funcional explicita"));
        }
        else if (string.IsNullOrWhiteSpace(sourcePath))
        {
            errors.Add(ErrorMessage(operationKey, parameterPath, sourcePath, "regla activa sin fuente funcional"));
        }
        else if (string.Equals(sourcePath, "constant.value", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(ErrorMessage(operationKey, parameterPath, sourcePath, "constant.value no es fuente funcional para una regla no constante"));
        }
        else if (IsAmbiguousFunctionalSource(operationKey, parameterPath, sourcePath))
        {
            errors.Add(ErrorMessage(operationKey, parameterPath, sourcePath, "fuente funcional ambigua para el contrato SOAP; requiere definicion funcional"));
        }

        if (defaultValue is not null)
        {
            if (BlocksReadinessDefault(operationKey, parameterPath, sourcePath, defaultValue))
            {
                errors.Add(ErrorMessage(operationKey, parameterPath, defaultValue, "default generico puede cubrir un parametro funcional critico"));
            }
            else if (IsPlaceholder(defaultValue) || LooksLikeSeededDate(defaultValue))
            {
                warnings.Add(WarningMessage(operationKey, parameterPath, defaultValue, "default generico detectado; no bloquea porque existe fuente funcional activa, pero requiere validacion"));
            }
        }

        if (fixedValue is not null && (IsPlaceholder(fixedValue) || LooksLikeSeededDate(fixedValue)))
        {
            warnings.Add(WarningMessage(operationKey, parameterPath, fixedValue, "valor fijo detectado en regla con fuente; validar que no actue como fallback funcional"));
        }

        if (IsSemanticallyDoubtfulSource(operationKey, parameterPath, sourcePath))
        {
            warnings.Add(WarningMessage(operationKey, parameterPath, sourcePath, "fuente activa con semantica pendiente de confirmacion funcional"));
        }

        return new FunctionalCoverageAssessment(errors, warnings);
    }

    private static bool IsNonBlockingTechnicalConstant(string operationKey, string parameterPath, string? value)
        => string.Equals(operationKey, IntegrationGuaranteeConstants.ProcContrapartidas, StringComparison.OrdinalIgnoreCase)
            && string.Equals(parameterPath, "OFDIRECCIONIP", StringComparison.OrdinalIgnoreCase)
            && string.Equals(value, "0.0.0.0", StringComparison.OrdinalIgnoreCase);

    private static bool IsReservedResponsePlaceholder(string operationKey, string parameterPath)
        => string.Equals(operationKey, IntegrationGuaranteeConstants.ProcTransacciones, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(parameterPath, "RTAACH", StringComparison.OrdinalIgnoreCase)
                || string.Equals(parameterPath, "RTALOC", StringComparison.OrdinalIgnoreCase));

    private static bool IsAllowedFunctionalConstant(string operationKey, string parameterPath, string? value)
        => string.Equals(operationKey, IntegrationGuaranteeConstants.ProcTransacciones, StringComparison.OrdinalIgnoreCase)
            && string.Equals(parameterPath, "TREG", StringComparison.OrdinalIgnoreCase)
            && string.Equals(value, "6", StringComparison.OrdinalIgnoreCase);

    private static bool IsAmbiguousFunctionalSource(string operationKey, string parameterPath, string? sourcePath)
        => string.Equals(operationKey, IntegrationGuaranteeConstants.ProcContrapartidas, StringComparison.OrdinalIgnoreCase)
            && string.Equals(parameterPath, "OFCTA", StringComparison.OrdinalIgnoreCase)
            && string.Equals(sourcePath, "transaction.originatingdfi", StringComparison.OrdinalIgnoreCase);

    private static bool BlocksReadinessDefault(string operationKey, string parameterPath, string? sourcePath, string value)
        => string.Equals(operationKey, IntegrationGuaranteeConstants.ProcContrapartidas, StringComparison.OrdinalIgnoreCase)
            && string.Equals(parameterPath, "OFCTA", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(sourcePath, "transaction.originatingdfi", StringComparison.OrdinalIgnoreCase)
                || IsPlaceholder(value));

    private static bool IsSemanticallyDoubtfulSource(string operationKey, string parameterPath, string? sourcePath)
        => string.Equals(operationKey, IntegrationGuaranteeConstants.ProcTransacciones, StringComparison.OrdinalIgnoreCase)
            && string.Equals(parameterPath, "LIBRE1", StringComparison.OrdinalIgnoreCase)
            && string.Equals(sourcePath, "fileControls.blockCount", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlaceholder(string value)
        => PlaceholderValues.Contains(value.Trim());

    private static bool LooksLikeSeededDate(string value)
        => DateTime.TryParse(value, out _);

    private static string? NormalizeValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ErrorMessage(string operationKey, string parameterPath, string? value, string reason)
        => $"{operationKey}.{parameterPath}: {reason}. Valor/Fuente: {FormatValue(value)}.";

    private static string WarningMessage(string operationKey, string parameterPath, string? value, string reason)
        => $"{operationKey}.{parameterPath}: {reason}. Valor/Fuente: {FormatValue(value)}.";

    private static string FormatValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? "(vacio)" : value;

    private static string FormatList(IReadOnlyCollection<string> values)
        => values.Count == 0 ? "(ninguno)" : string.Join(", ", values);

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
            FallbackFields: [],
            RequiredFallbackFields: [],
            UsesFallback: false,
            CanBuildPayload: false,
            Errors: errors,
            Warnings: []);

    private sealed record FunctionalCoverageAssessment(
        IReadOnlyCollection<string> Errors,
        IReadOnlyCollection<string> Warnings);
}
