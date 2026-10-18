using System.Globalization;
using System.Text.Json;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Integrations.Registries;
using Cfa.ACHInterbank.Application.Integrations.Transformations;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Providers;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class IntegrationMappingPreviewService : IIntegrationMappingPreviewService
{
    private static readonly string[] SensitivePathFragments =
    [
        "account",
        "cuenta",
        "identification",
        "identificacion",
        "customer",
        "receivercustomer",
        "reference",
        "trace",
        "email",
        "phone",
        "document",
        "token",
        "secret",
        "password",
        "credential",
        "authorization",
        "ofcta",
        "ofnit",
        "ofidtx"
    ];

    private static readonly string[] SecretValueMarkers =
    [
        "bearer ",
        "password=",
        "passwd=",
        "secret=",
        "token=",
        "api_key",
        "apikey",
        "authorization:"
    ];

    private readonly AchDbContext _context;
    private readonly IIntegrationMappingValidationService _validationService;
    private readonly IIntegrationTransformationRegistry _transformationRegistry;

    public IntegrationMappingPreviewService(
        AchDbContext context,
        IIntegrationMappingValidationService? validationService = null,
        IIntegrationTransformationRegistry? transformationRegistry = null)
    {
        _context = context;
        _validationService = validationService ?? new IntegrationMappingValidationService(context);
        _transformationRegistry = transformationRegistry
            ?? new IntegrationTransformationRegistry(StandardIntegrationTransformationCatalog.Create());
    }

    public async Task<IntegrationMappingPreviewResultDto> PreviewAsync(
        Guid mappingSetId,
        PreviewIntegrationMappingSetRequest request,
        CancellationToken ct = default)
    {
        var set = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == mappingSetId, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet {mappingSetId}.");

        var method = await _context.Set<IntegrationMethod>()
            .AsNoTracking()
            .FirstAsync(x => x.Id == set.MethodId, ct);

        var rules = await _context.Set<IntegrationMappingRule>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == mappingSetId && x.Enabled)
            .OrderBy(x => x.ParameterId)
            .ThenBy(x => x.Priority)
            .ToListAsync(ct);

        IntegrationContractVersion? contract = null;
        if (set.ContractVersionId.HasValue)
        {
            contract = await _context.IntegrationContractVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == set.ContractVersionId.Value, ct);
        }

        var parameterQuery = _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == set.MethodId && x.IsActive);
        parameterQuery = contract is not null
            ? IsBaselineBridge(contract)
                ? parameterQuery.Where(x => x.ContractVersionId == contract.Id || x.ContractVersionId == null)
                : parameterQuery.Where(x => x.ContractVersionId == contract.Id)
            : parameterQuery.Where(x => x.ContractVersionId == null);
        var parameters = await parameterQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var sourceCatalog = await _context.Set<IntegrationSourceCatalogField>()
            .AsNoTracking()
            .Where(x => x.MethodId == set.MethodId && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        var validation = await _validationService.EvaluateAsync(mappingSetId, includeWarnings: true, ct);
        var validationByParameter = validation.Parameters.ToDictionary(x => x.ParameterId);
        var transformationByCode = _transformationRegistry.Descriptors
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var sourceProviders = new CurrentIntegrationSourceProviderFactory(_context).Create();
        var sourceSensitivity = BuildSourceSensitivityIndex(sourceProviders);

        var (tx, contextMode) = await LoadSampleTransactionAsync(request, ct);
        var cycle = tx?.AchCycle;
        var batch = tx?.AchBatch;
        var clearingHouse = cycle?.ClearingHouse;
        var addenda = tx?.Addendas.OrderBy(a => a.SequenceNumber).FirstOrDefault();
        var previewNow = DateTime.UtcNow;

        var previewItems = new List<IntegrationMappingPreviewItemDto>();
        var payload = new Dictionary<string, string?>();

        foreach (var parameter in parameters)
        {
            var winner = rules
                .Where(x => x.ParameterId == parameter.Id)
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            validationByParameter.TryGetValue(parameter.Id, out var parameterValidation);
            var parameterIssues = validation.Issues
                .Where(issue =>
                    string.Equals(issue.Path, parameter.ParameterPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(issue.Path, $"Parameter:{parameter.Id}", StringComparison.OrdinalIgnoreCase))
                .ToList();

            string? sanitizedValue = null;
            var isMasked = false;
            var resolvedFrom = "Sin origen configurado";
            var sourceSection = "sin-configurar";
            var resolutionKind = parameterValidation?.ResolutionKind ?? "none";
            string? appliedTransformation = null;
            string? sourceDisplayName = null;
            string? transformationDisplayName = null;
            var priority = 0;
            var enabled = false;

            if (winner is not null)
            {
                var resolvedSource = await ResolvePreviewValueAsync(
                    winner,
                    method.Code,
                    tx,
                    addenda,
                    batch,
                    cycle,
                    clearingHouse,
                    previewNow,
                    contextMode,
                    sourceProviders,
                    ct);
                var usedConfiguredFallback = !string.IsNullOrWhiteSpace(winner.DefaultValue)
                    && string.Equals(resolvedSource, winner.DefaultValue, StringComparison.Ordinal);
                var resolved = ApplyTransformation(
                    method.Code,
                    parameter.ParameterPath,
                    parameter.DataType,
                    winner.TransformationCode,
                    winner.FormatMask,
                    resolvedSource);

                resolvedFrom = ResolveFromLabel(winner);
                sourceSection = ResolveSection(winner.SourceKind);
                resolutionKind = parameterValidation?.ResolutionKind ?? ResolveResolutionKind(winner);
                appliedTransformation = winner.TransformationCode;
                priority = winner.Priority;
                enabled = winner.Enabled;

                sourceDisplayName = ResolveSourceDisplayName(winner, sourceCatalog);
                transformationDisplayName = ResolveTransformationDisplayName(winner.TransformationCode, transformationByCode);

                var classification = usedConfiguredFallback
                    ? IntegrationSensitiveClassification.Personal
                    : ResolveSensitiveClassification(winner, parameter.ParameterPath, sourceSensitivity);
                var sanitized = SanitizePreviewValue(
                    resolved,
                    classification,
                    winner.SourceFieldPath,
                    parameter.ParameterPath);
                sanitizedValue = sanitized.Value;
                isMasked = sanitized.IsMasked;
            }

            payload[parameter.ParameterPath] = sanitizedValue;
            previewItems.Add(new IntegrationMappingPreviewItemDto(
                parameter.Id,
                parameter.ParameterPath,
                resolvedFrom,
                sanitizedValue,
                sourceSection,
                resolutionKind,
                appliedTransformation,
                priority,
                enabled,
                string.IsNullOrWhiteSpace(parameter.DisplayName) ? "Dato CFA sin nombre funcional" : parameter.DisplayName,
                parameter.DescriptionEs ?? string.Empty,
                parameter.Required,
                sourceDisplayName,
                transformationDisplayName,
                parameterValidation?.Status ?? "inactive",
                parameterIssues,
                isMasked));
        }

        var limitedItems = previewItems.Take(Math.Max(1, request.MaxItems)).ToList();
        var rawJson = JsonSerializer.Serialize(limitedItems);
        var payloadJson = JsonSerializer.Serialize(payload.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value));

        return new IntegrationMappingPreviewResultDto(
            mappingSetId,
            method.Id,
            method.Code,
            contextMode,
            previewItems,
            payloadJson,
            rawJson,
            validation);
    }

    private static Dictionary<string, IntegrationSensitiveClassification> BuildSourceSensitivityIndex(
        IReadOnlyCollection<IIntegrationSourceValueProvider> providers)
    {
        var index = new Dictionary<string, IntegrationSensitiveClassification>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            var descriptor = provider.Descriptor;
            index[SourceSensitivityKey(descriptor.SourceKind, descriptor.SourceKey)] = descriptor.SensitiveClassification;
        }

        return index;
    }

    private async Task<(AchTransaction? tx, string contextMode)> LoadSampleTransactionAsync(
        PreviewIntegrationMappingSetRequest request,
        CancellationToken ct)
    {
        if (request.UseControlledSample)
        {
            return (BuildControlledSample(), "controlled-sample");
        }

        if (request.SampleTransactionId.HasValue)
        {
            var txById = await _context.AchTransactions
                .AsNoTracking()
                .Include(t => t.Addendas)
                .Include(t => t.AchBatch)
                .Include(t => t.AchCycle)
                    .ThenInclude(c => c!.ClearingHouse)
                .FirstOrDefaultAsync(t => t.Id == request.SampleTransactionId.Value, ct);

            return (txById, "real-transaction");
        }

        var query = _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.Addendas)
            .Include(t => t.AchBatch)
            .Include(t => t.AchCycle)
                .ThenInclude(c => c!.ClearingHouse)
            .OrderByDescending(t => t.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SampleCycleId))
        {
            query = query.Where(t => t.AchCycleId == request.SampleCycleId);
        }

        var tx = await query.FirstOrDefaultAsync(ct);
        return tx is null ? (BuildControlledSample(), "controlled-sample") : (tx, "real-latest");
    }

    private static AchTransaction BuildControlledSample()
        => new()
        {
            Id = 999001,
            Amount = 1234.56m,
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            TraceNumber = "000123456789012",
            Reference = "PREVIEW-TRACE",
            OriginatingDFI = "021000021",
            ReceivingDFI = "031100209",
            CompanyIdentification = "COMPANY123",
            EffectiveEntryDate = DateTime.UtcNow.Date,
            SourceInstitutionId = 100,
            DestinationInstitutionId = 200,
            SourceAccountNumber = "123456789012",
            DestinationAccountNumber = "210987654321",
            AchBatch = new AchBatch { Id = 7001 },
            AchCycle = new AchCycle
            {
                Id = "CYCLE-PREVIEW",
                CycleName = "Ciclo Preview",
                ProcessingDate = DateTime.UtcNow.Date,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(18),
                CutoffTime = TimeSpan.FromHours(16),
                ClearingHouse = new ClearingHouse { Id = 12, Code = "ACH-TEST", Name = "ACH Demo", OriginCode = "ORG" }
            },
            Addendas =
            [
                new AchTransactionAddenda
                {
                    SequenceNumber = 1,
                    AddendaType = "05",
                    BusinessType = AchAddendaBusinessType.Credit,
                    Information = "Pago de prueba",
                    Purpose = "Preview",
                    Reference = "ADD-001",
                    CollectorId = "COL-1",
                    ReceiverCustomerCode = "RC-1",
                    ServiceDescription = "Servicio demo"
                }
            ]
        };

    private static string ResolveSourceDisplayName(
        IntegrationMappingRule rule,
        IReadOnlyDictionary<long, IntegrationSourceCatalogField> sourceCatalog)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return "Valor fijo configurado";
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Constant)
        {
            return "Valor configurado";
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Expression)
        {
            return "Configuración avanzada";
        }

        return rule.SourceCatalogFieldId.HasValue
               && sourceCatalog.TryGetValue(rule.SourceCatalogFieldId.Value, out var field)
            ? field.DisplayName
            : "Origen configurado";
    }

    private static string? ResolveTransformationDisplayName(
        string? transformationCode,
        IReadOnlyDictionary<string, IntegrationTransformationDescriptor> transformationByCode)
    {
        if (string.IsNullOrWhiteSpace(transformationCode))
        {
            return null;
        }

        return transformationByCode.TryGetValue(transformationCode, out var descriptor)
            ? descriptor.DisplayNameEs
            : transformationCode;
    }

    private static string ResolveFromLabel(IntegrationMappingRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return "FixedValue";
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Constant)
        {
            return "Default/Constant";
        }

        return rule.SourceKind switch
        {
            IntegrationSourceKindEnum.Expression => "Expression",
            _ => string.IsNullOrWhiteSpace(rule.SourceFieldPath)
                ? $"CatalogField:{rule.SourceCatalogFieldId?.ToString() ?? "n/a"}"
                : rule.SourceFieldPath
        };
    }

    private static string ResolveSection(IntegrationSourceKindEnum sourceKind)
        => sourceKind switch
        {
            IntegrationSourceKindEnum.Cycle or IntegrationSourceKindEnum.ClearingHouse => "ciclo-camara",
            IntegrationSourceKindEnum.Transaction => "transaccion",
            IntegrationSourceKindEnum.Batch => "lote",
            IntegrationSourceKindEnum.Addenda => "addenda",
            _ => "configuracion"
        };

    private static string ResolveResolutionKind(IntegrationMappingRule winner)
    {
        if (!string.IsNullOrWhiteSpace(winner.FixedValue) || !string.IsNullOrWhiteSpace(winner.DefaultValue))
        {
            return "default-fixed";
        }

        if (winner.SourceKind == IntegrationSourceKindEnum.Expression)
        {
            return "expression";
        }

        return "source-field";
    }

    private string? ApplyTransformation(
        string methodCode,
        string parameterPath,
        string targetDataType,
        string? transformationCode,
        string? formatMask,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(transformationCode))
        {
            return value;
        }

        try
        {
            var transformation = _transformationRegistry.GetRequired(transformationCode);
            var transformed = transformation.Apply(
                value,
                new IntegrationTransformationContext(methodCode, parameterPath, targetDataType, formatMask));
            return ToInvariantString(transformed);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException or NotSupportedException)
        {
            // La validación autoritativa ya reporta la incompatibilidad. El preview conserva el valor
            // de origen para sanitizarlo, sin simular en frontend ni convertir el fallo en exposición técnica.
            return value;
        }
    }

    private async Task<string?> ResolvePreviewValueAsync(
        IntegrationMappingRule rule,
        string methodCode,
        AchTransaction? tx,
        AchTransactionAddenda? addenda,
        AchBatch? batch,
        AchCycle? cycle,
        ClearingHouse? clearingHouse,
        DateTime executionDateTime,
        string contextMode,
        IReadOnlyCollection<IIntegrationSourceValueProvider> providers,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue)
            || rule.SourceKind is IntegrationSourceKindEnum.Constant or IntegrationSourceKindEnum.Expression
            || string.Equals(contextMode, "controlled-sample", StringComparison.Ordinal))
        {
            return ResolvePreviewValue(rule, tx, addenda, batch, cycle, clearingHouse, executionDateTime);
        }

        if (!string.IsNullOrWhiteSpace(rule.SourceFieldPath))
        {
            var provider = providers.FirstOrDefault(item =>
                item.Descriptor.SourceKind == rule.SourceKind
                && string.Equals(item.Descriptor.SourceKey, rule.SourceFieldPath, StringComparison.OrdinalIgnoreCase)
                && item.Descriptor.SupportsMethod(methodCode));
            if (provider is not null)
            {
                try
                {
                    var source = new IntegrationSourceReference(rule.SourceKind, rule.SourceFieldPath);
                    var context = new IntegrationExecutionContext(
                        methodCode,
                        $"preview:{Guid.NewGuid():N}",
                        DateTime.SpecifyKind(executionDateTime, DateTimeKind.Utc),
                        tx?.Id,
                        cycle?.Id);
                    var resolved = await provider.ResolveAsync(source, context, ct);
                    var value = ToInvariantString(resolved);
                    return string.IsNullOrWhiteSpace(value) ? rule.DefaultValue : value;
                }
                catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
                {
                    // Algunas fuentes requieren contexto adicional no disponible para una muestra. Se usa
                    // el resolvedor de preview acotado y luego se aplica sanitización backend obligatoria.
                }
            }
        }

        return ResolvePreviewValue(rule, tx, addenda, batch, cycle, clearingHouse, executionDateTime);
    }

    private static string? ToInvariantString(object? value)
        => value switch
        {
            null => null,
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

    private static string? ResolvePreviewValue(
        IntegrationMappingRule rule,
        AchTransaction? tx,
        AchTransactionAddenda? addenda,
        AchBatch? batch,
        AchCycle? cycle,
        ClearingHouse? clearingHouse,
        DateTime executionDateTime)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return rule.FixedValue;
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Constant)
        {
            return rule.DefaultValue;
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Expression)
        {
            return "[configuración avanzada]";
        }

        var path = rule.SourceFieldPath?.Trim().ToLowerInvariant() ?? string.Empty;
        var raw = path switch
        {
            "transaction.id" => tx?.Id.ToString(),
            "transaction.transactionexternalid" => tx?.TransactionExternalId,
            "transaction.amount" => tx?.Amount.ToString(),
            "transaction.type" => tx?.Type.ToString(),
            "transaction.transactioncode" => tx?.TransactionCode,
            "transaction.tracenumber" => tx?.TraceNumber,
            "transaction.tracesequencenumber" => tx?.TraceSequenceNumber.ToString(),
            "transaction.reference" => tx?.Reference,
            "transaction.originatingdfi" => tx?.OriginatingDFI,
            "transaction.receivingdfi" => tx?.ReceivingDFI,
            "transaction.companyidentification" => tx?.CompanyIdentification,
            "transaction.sourceaccountnumber" => tx?.SourceAccountNumber,
            "transaction.destinationaccountnumber" => tx?.DestinationAccountNumber,
            "transaction.debitcreditindicator" => tx is null
                ? null
                : tx.Type == TransactionTypeEnum.Debit ? "D" : tx.Type == TransactionTypeEnum.Credit ? "C" : null,
            "transaction.effectiveentrydate" => tx?.EffectiveEntryDate.ToString("O"),
            "transaction.sourceinstitutionid" => tx?.SourceInstitutionId.ToString(),
            "transaction.destinationinstitutionid" => tx?.DestinationInstitutionId.ToString(),
            "addenda.sequencenumber" => addenda?.SequenceNumber?.ToString(),
            "addenda.addendatype" => addenda?.AddendaType,
            "addenda.businesstype" => addenda?.BusinessType.ToString(),
            "addenda.information" => addenda?.Information,
            "addenda.purpose" => addenda?.Purpose,
            "addenda.reference" => addenda?.Reference,
            "addenda.collectorid" => addenda?.CollectorId,
            "addenda.receivercustomercode" => addenda?.ReceiverCustomerCode,
            "addenda.servicedescription" => addenda?.ServiceDescription,
            "addenda.returnreasoncode" => addenda?.ReturnReasonCode,
            "addenda.originaltracenumber" => addenda?.OriginalTraceNumber,
            "addenda.newtracenumber" => addenda?.NewTraceNumber,
            "batch.id" => batch?.Id.ToString(),
            "cycle.id" => cycle?.Id,
            "cycle.cyclename" => cycle?.CycleName,
            "cycle.processingdate" => cycle?.ProcessingDate.ToString("O"),
            "cycle.starttime" => cycle?.StartTime.ToString(),
            "cycle.endtime" => cycle?.EndTime.ToString(),
            "cycle.cutofftime" => cycle?.CutoffTime.ToString(),
            "clearinghouse.id" => clearingHouse?.Id.ToString(),
            "clearinghouse.code" => clearingHouse?.Code,
            "execution.datetimeutc" => executionDateTime.ToString("O"),
            "execution.dateyyyymmdd" => executionDateTime.ToString("yyyyMMdd"),
            _ => null
        };

        return string.IsNullOrWhiteSpace(raw) ? rule.DefaultValue : raw;
    }

    private static IntegrationSensitiveClassification ResolveSensitiveClassification(
        IntegrationMappingRule rule,
        string parameterPath,
        IReadOnlyDictionary<string, IntegrationSensitiveClassification> sourceSensitivity)
    {
        if (rule.SourceKind == IntegrationSourceKindEnum.Constant || !string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            // Los valores configurados pueden contener datos reales introducidos por operadores.
            // Sin metadata de sensibilidad propia, el preview adopta clasificación conservadora.
            return IntegrationSensitiveClassification.Personal;
        }

        if (!string.IsNullOrWhiteSpace(rule.SourceFieldPath)
            && sourceSensitivity.TryGetValue(
                SourceSensitivityKey(rule.SourceKind, rule.SourceFieldPath),
                out var classification))
        {
            return classification;
        }

        return rule.SourceKind switch
        {
            IntegrationSourceKindEnum.Cycle or IntegrationSourceKindEnum.ClearingHouse or IntegrationSourceKindEnum.Batch
                => IntegrationSensitiveClassification.Technical,
            IntegrationSourceKindEnum.Transaction or IntegrationSourceKindEnum.Addenda or IntegrationSourceKindEnum.DifferentialResponse
                => IntegrationSensitiveClassification.Personal,
            _ => IsSensitivePath(parameterPath)
                ? IntegrationSensitiveClassification.Personal
                : IntegrationSensitiveClassification.Technical
        };
    }

    private static SanitizedPreviewValue SanitizePreviewValue(
        string? value,
        IntegrationSensitiveClassification classification,
        string? sourceKey,
        string parameterPath)
    {
        if (value is null)
        {
            return new(null, false);
        }

        if (ContainsSecretMarker(value))
        {
            return new("[valor protegido]", true);
        }

        var sensitivePath = IsSensitivePath(sourceKey) || IsSensitivePath(parameterPath);
        return classification switch
        {
            IntegrationSensitiveClassification.Account => new(MaskKeepingLast(value, 4), true),
            IntegrationSensitiveClassification.Personal => new("[dato personal protegido]", true),
            IntegrationSensitiveClassification.Financial when IsDateOrTimeSource(sourceKey)
                => SanitizeUnclassified(value, sensitivePath: false),
            IntegrationSensitiveClassification.Financial => new("[dato financiero protegido]", true),
            IntegrationSensitiveClassification.Technical when sensitivePath
                => new(MaskKeepingLast(value, 4), true),
            _ => SanitizeUnclassified(value, sensitivePath)
        };
    }

    private static SanitizedPreviewValue SanitizeUnclassified(string value, bool sensitivePath)
    {
        if (sensitivePath || LooksLikeLongIdentifier(value))
        {
            return new(MaskKeepingLast(value, 4), true);
        }

        if (value.Contains('@'))
        {
            return new("[dato personal protegido]", true);
        }

        return new(value, false);
    }

    private static bool IsSensitivePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return SensitivePathFragments.Any(normalized.Contains);
    }

    private static bool IsDateOrTimeSource(string? sourceKey)
    {
        var normalized = sourceKey?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized.Contains("date", StringComparison.Ordinal)
               || normalized.Contains("time", StringComparison.Ordinal)
               || normalized.Contains("fecha", StringComparison.Ordinal);
    }

    private static bool ContainsSecretMarker(string value)
    {
        var normalized = value.ToLowerInvariant();
        return SecretValueMarkers.Any(normalized.Contains);
    }

    private static bool LooksLikeLongIdentifier(string value)
    {
        var lettersOrDigits = value.Count(char.IsLetterOrDigit);
        var digits = value.Count(char.IsDigit);
        return value.Length >= 12 && (digits >= 8 || lettersOrDigits >= 16);
    }

    private static string MaskKeepingLast(string value, int visibleCharacters)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var compact = value.Trim();
        if (compact.Length <= visibleCharacters)
        {
            return "••••";
        }

        return $"••••{compact[^visibleCharacters..]}";
    }

    private static bool IsBaselineBridge(IntegrationContractVersion contract)
        => contract.SourceType == IntegrationContractSourceTypeEnum.BaselineMigration
           && string.Equals(contract.VersionLabel, "baseline-v1", StringComparison.Ordinal);

    private static string SourceSensitivityKey(IntegrationSourceKindEnum sourceKind, string sourceKey)
        => $"{sourceKind}:{sourceKey.Trim()}";

    private sealed record SanitizedPreviewValue(string? Value, bool IsMasked);
}
