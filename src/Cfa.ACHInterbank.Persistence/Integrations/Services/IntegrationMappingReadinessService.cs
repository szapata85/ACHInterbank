using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
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
    private readonly IntegrationMappingSnapshotBuilder _snapshotBuilder;

    public IntegrationMappingReadinessService(
        AchDbContext context,
        IIntegrationCatalogService catalogService,
        IntegrationMappingSnapshotBuilder? snapshotBuilder = null)
    {
        _context = context;
        _catalogService = catalogService;
        _snapshotBuilder = snapshotBuilder ?? new IntegrationMappingSnapshotBuilder(context);
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

        var publishedMappings = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);

        if (publishedMappings.Count == 0)
        {
            var requiredFallbackFields = activeInputParameters
                .Where(x => x.Required)
                .Select(x => x.ParameterPath)
                .ToList();

            return new IntegrationMappingReadinessResult(
                IsReady: false,
                Status: "Failed",
                Code: "PUBLISHED_MAPPING_NOT_FOUND",
                IntegrationKey: integrationKey,
                OperationKey: operationKey,
                MappingPurpose: mappingPurpose,
                MappingDirection: mappingDirection,
                RequiredMappings: requiredFallbackFields.Count,
                ActiveMappings: 0,
                MissingRequiredMappings: requiredFallbackFields,
                InactiveRequiredMappings: [],
                FallbackFields: [],
                RequiredFallbackFields: [],
                UsesFallback: false,
                CanBuildPayload: false,
                Errors: [$"No existe IntegrationMappingSet publicado para {methodCode}; no se permite fallback para campos requeridos."],
                Warnings: []);
        }

        if (publishedMappings.Count != 1)
        {
            return Failed(integrationKey, operationKey, mappingPurpose, mappingDirection,
                "MAPPING_SET_NOT_UNIQUE",
                [$"Existen {publishedMappings.Count} IntegrationMappingSets publicados y activos para {methodCode}."]);
        }

        var published = publishedMappings[0];
        var snapshot = await _snapshotBuilder.BuildAsync(published.Id, ct);

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
                Warnings: [])
            {
                MappingSetId = snapshot.MappingSetId,
                MappingVersion = snapshot.Version,
                MappingSnapshotHash = snapshot.SnapshotHash
            };
        }

        if (string.Equals(operationKey, IntegrationGuaranteeConstants.ProcTransacciones, StringComparison.OrdinalIgnoreCase)
            && transactionId.HasValue)
        {
            functionalErrors.AddRange(await ValidateProcTransaccionesRuntimeAsync(transactionId.Value, ct));
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
                Warnings: functionalWarnings)
            {
                MappingSetId = snapshot.MappingSetId,
                MappingVersion = snapshot.Version,
                MappingSnapshotHash = snapshot.SnapshotHash
            };
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
                Warnings: functionalWarnings)
            {
                MappingSetId = snapshot.MappingSetId,
                MappingVersion = snapshot.Version,
                MappingSnapshotHash = snapshot.SnapshotHash
            };
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
            Warnings: [])
        {
            MappingSetId = snapshot.MappingSetId,
            MappingVersion = snapshot.Version,
            MappingSnapshotHash = snapshot.SnapshotHash
        };
    }

    private async Task<IReadOnlyCollection<string>> ValidateProcTransaccionesRuntimeAsync(int transactionId, CancellationToken ct)
    {
        var errors = new List<string>();
        var transaction = await _context.AchTransactions
            .AsNoTracking()
            .Include(x => x.SourceInstitution)
            .Include(x => x.DestinationInstitution)
            .Include(x => x.AchCycle)
                .ThenInclude(x => x.ClearingHouse)
            .SingleOrDefaultAsync(x => x.Id == transactionId, ct);
        if (transaction is null)
        {
            return [$"Proc_Transacciones.transaction: no existe la transacción {transactionId} para evaluar fuentes funcionales."];
        }

        if (!IsTransitCode(transaction.DestinationInstitution?.TransitCode))
            errors.Add("Proc_Transacciones.BCORECEP: la CFA receptora no tiene TransitCode numérico de tres dígitos.");
        if (!IsTransitCode(transaction.SourceInstitution?.TransitCode))
            errors.Add("Proc_Transacciones.BCOORIG: la institución originadora no tiene TransitCode numérico de tres dígitos.");

        var clearingHouse = transaction.AchCycle?.ClearingHouse;
        var validClearingHouse = clearingHouse is not null
            && ((clearingHouse.Code == "ACHCOL" && clearingHouse.Id == 1)
                || (clearingHouse.Code == "CENIT" && clearingHouse.Id == 2));
        if (!validClearingHouse)
            errors.Add("Proc_Transacciones.IDCAMCOMPE: la cámara no está resuelta con IDs canónicos ACHCOL=1 o CENIT=2.");

        var link = await _context.IncomingNachaTransactionLinks
            .AsNoTracking()
            .Include(x => x.EntryDetail)
            .Include(x => x.AddendaRecord)
            .Where(x => x.AchTransactionId == transactionId && x.IsFinal)
            .OrderByDescending(x => x.LinkedAtUtc)
            .FirstOrDefaultAsync(ct);
        var entry = link?.EntryDetail;
        var addenda = link?.AddendaRecord;
        var batch = entry is null || string.IsNullOrWhiteSpace(entry.NachaID)
            ? null
            : await _context.BatchHeaders.AsNoTracking().SingleOrDefaultAsync(
                x => x.NachaID == entry.NachaID && x.BatchNumber == entry.BatchNumber,
                ct);

        try
        {
            _ = ProcTransaccionesRequestMapper.ToFunctionalBatchId(batch?.BatchNumber.ToString("D7") ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add($"Proc_Transacciones.IDLOTE: {ex.Message}");
        }

        try
        {
            _ = ProcTransaccionesPaymentInformationBuilder.Build(
                transaction.CompanyIdentification,
                batch?.CompanyEntryDescription ?? string.Empty,
                addenda?.PaymentRelatedInformation ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add($"Proc_Transacciones.DESTRAN: {ex.Message}");
            errors.Add($"Proc_Transacciones.INFPAG: {ex.Message}");
        }

        return errors;
    }

    private static bool IsTransitCode(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length == 3 && normalized.All(char.IsDigit);
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
    {
        if (string.Equals(operationKey, IntegrationGuaranteeConstants.ProcTransacciones, StringComparison.OrdinalIgnoreCase))
        {
            return (string.Equals(parameterPath, "DISCRE", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(value, "V", StringComparison.OrdinalIgnoreCase))
                || (string.Equals(parameterPath, "IREVER", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(value, "0", StringComparison.OrdinalIgnoreCase));
        }

        if (!string.Equals(operationKey, IntegrationGuaranteeConstants.ProcContrapartidas, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return parameterPath.ToUpperInvariant() switch
        {
            "OFDD" => string.Equals(value, "TRANSFER", StringComparison.OrdinalIgnoreCase),
            "OFMONCRE" => string.Equals(value, "0", StringComparison.OrdinalIgnoreCase),
            "OFST" => string.Equals(value, "OO", StringComparison.OrdinalIgnoreCase),
            "OFIDTX" => string.Equals(value, "0", StringComparison.OrdinalIgnoreCase),
            "OFIDREVER" => string.Equals(value, "0", StringComparison.OrdinalIgnoreCase),
            "OFIDEBAPLI" => string.Equals(value, "1", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool IsAmbiguousFunctionalSource(string operationKey, string parameterPath, string? sourcePath)
    {
        if (string.Equals(operationKey, IntegrationGuaranteeConstants.ProcTransacciones, StringComparison.OrdinalIgnoreCase))
        {
            return parameterPath.ToUpperInvariant() switch
            {
                "BCORECEP" => !string.Equals(sourcePath, "destinationInstitution.transitCodeNormalized", StringComparison.OrdinalIgnoreCase),
                "BCOORIG" => !string.Equals(sourcePath, "sourceInstitution.transitCodeNormalized", StringComparison.OrdinalIgnoreCase),
                "DESTRAN" or "INFPAG" => !string.Equals(sourcePath, "procTransacciones.paymentInformation", StringComparison.OrdinalIgnoreCase),
                "IDTRAN" => !string.Equals(sourcePath, "transaction.traceSequenceNumber", StringComparison.OrdinalIgnoreCase),
                "IDLOTE" => !string.Equals(sourcePath, "procTransacciones.functionalBatchId", StringComparison.OrdinalIgnoreCase),
                "IDCAMCOMPE" => !string.Equals(sourcePath, "cycle.clearingHouseId", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        return string.Equals(operationKey, IntegrationGuaranteeConstants.ProcContrapartidas, StringComparison.OrdinalIgnoreCase)
            && string.Equals(parameterPath, "OFCTA", StringComparison.OrdinalIgnoreCase)
            && string.Equals(sourcePath, "transaction.originatingdfi", StringComparison.OrdinalIgnoreCase);
    }

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

    private static IntegrationMappingReadinessResult AttachSnapshotIdentity(
        IntegrationMappingReadinessResult result,
        IntegrationMappingSnapshotBuilder.IntegrationMappingSnapshotResult snapshot)
        => result with
        {
            MappingSetId = snapshot.MappingSetId,
            MappingVersion = snapshot.Version,
            MappingSnapshotHash = snapshot.SnapshotHash
        };

    private sealed record FunctionalCoverageAssessment(
        IReadOnlyCollection<string> Errors,
        IReadOnlyCollection<string> Warnings);
}
