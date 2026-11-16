using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class ProcContrapartidasFunctionalMappingResolver : IProcContrapartidasFunctionalMappingResolver
{
    private readonly AchDbContext _context;

    public ProcContrapartidasFunctionalMappingResolver(AchDbContext context)
    {
        _context = context;
    }

    public async Task<ProcContrapartidasRequestResolution?> TryResolveAsync(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime,
        CancellationToken ct = default)
    {
        if (transactions.Count != 1)
        {
            throw new InvalidOperationException("PROC_CONTRAPARTIDAS_CARDINALITY: el resolver requiere exactamente una transacción.");
        }

        var debit = transactions.Single();
        if (debit.Type != TransactionTypeEnum.Debit || debit.IsPrenotification || debit.Amount <= 0m)
        {
            throw new InvalidOperationException(
                "PROC_CONTRAPARTIDAS_DEBIT_ONLY: se requiere un debito monetario positivo.");
        }

        await new IntegrationMappingBootstrapper(_context).EnsureAsync(ct);

        var method = await _context.Set<IntegrationMethod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas" && x.IsActive, ct);

        if (method is null)
        {
            return null;
        }

        var publishedMappings = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);

        if (publishedMappings.Count == 0)
        {
            return null;
        }

        if (publishedMappings.Count != 1)
        {
            throw new InvalidOperationException(
                $"INTEGRATION_MAPPING_NOT_UNIQUE: existen {publishedMappings.Count} mappings publicados activos para Proc_Contrapartidas.");
        }

        var published = publishedMappings[0];

        var parameters = await _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive && x.Direction == IntegrationParameterDirectionEnum.Input)
            .ToListAsync(ct);

        var rules = await _context.Set<IntegrationMappingRule>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == published.Id && x.Enabled)
            .OrderBy(x => x.Priority)
            .ToListAsync(ct);

        if (rules.Count == 0)
        {
            throw new InvalidOperationException(
                $"El MappingSet publicado {published.Id} (v{published.Version}) no tiene reglas habilitadas.");
        }

        var sourceCatalog = await _context.Set<IntegrationSourceCatalogField>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        var tx = transactions.Single();
        if (cycle.ClearingHouse is null)
        {
            cycle.ClearingHouse = await _context.Set<ClearingHouse>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == cycle.ClearingHouseId, ct);
        }
        var resolved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in parameters)
        {
            var winner = rules
                .Where(r => r.ParameterId == parameter.Id)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Id)
                .FirstOrDefault();

            if (winner is null)
            {
                if (parameter.Required)
                {
                    throw new InvalidOperationException(
                        $"INTEGRATION_MAPPING_REQUIRED: el mapping publicado no contiene una regla activa para {parameter.ParameterPath}.");
                }
                continue;
            }

            // Reject unaccredited contract identities and protected account/reference overrides.
            // Published custom mappings remain stored unchanged and fail closed.
            var sourcePath = !string.IsNullOrWhiteSpace(winner.SourceFieldPath)
                ? winner.SourceFieldPath
                : (winner.SourceCatalogFieldId.HasValue
                    && sourceCatalog.TryGetValue(winner.SourceCatalogFieldId.Value, out var sourceField)
                        ? sourceField.FieldPath : string.Empty);
            var safetyError = AssessProtectedRule(parameter.ParameterPath, winner, sourcePath);
            if (safetyError is not null)
                throw new InvalidOperationException(safetyError);

            var ruleResolution = ResolveValue(winner, sourceCatalog, cycle, tx, executionDateTime);
            if (parameter.Required && ruleResolution.UsedDefault)
            {
                throw new InvalidOperationException(
                    $"INTEGRATION_MAPPING_SOURCE_REQUIRED: {parameter.ParameterPath} no se resolvio desde su fuente; no se permite fallback.");
            }
            if (parameter.Required && string.IsNullOrWhiteSpace(ruleResolution.Value))
            {
                throw new InvalidOperationException(
                    $"INTEGRATION_MAPPING_SOURCE_REQUIRED: la fuente obligatoria de {parameter.ParameterPath} no produjo un valor.");
            }

            resolved[parameter.ParameterPath] = ruleResolution.Value;
        }

        if (!resolved.ContainsKey("OFIDLOT"))
        {
            throw new InvalidOperationException(
                $"El MappingSet publicado {published.Id} no resolvió OFIDLOT, requerido por el contrato técnico.");
        }

        var contract = new ProcContrapartidasRequestContract
        {
            OFNIT = ResolveRequiredString("OFNIT"),
            OFEMP = ResolveRequiredString("OFEMP"),
            OFCTA = ResolveRequiredString("OFCTA"),
            OFDD = ResolveRequiredString("OFDD"),
            OFFECHEFEC = ResolveRequiredString("OFFECHEFEC"),
            OFMONDEB = ResolveRequiredDecimal("OFMONDEB"),
            OFMONCRE = ResolveRequiredDecimal("OFMONCRE"),
            OFIDARCH = ResolveRequiredInt("OFIDARCH"),
            OFIDLOT = ResolveRequiredInt("OFIDLOT"),
            OFST = ResolveRequiredString("OFST"),
            OFIDTX = ResolveRequiredString("OFIDTX"),
            OFIDREVER = ResolveRequiredInt("OFIDREVER"),
            OFIDEBAPLI = ResolveRequiredInt("OFIDEBAPLI"),
            OFIDCAMCOMPE = ResolveRequiredInt("OFIDCAMCOMPE"),
            OFDIRECCIONIP = ResolveRequiredString("OFDIRECCIONIP"),
            OFLIBRE = ResolveRequiredString("OFLIBRE"),
            OFLIBRE1 = ResolveRequiredInt("OFLIBRE1"),
            ANSIDLOTE = ResolveInt("ANSIDLOTE"),
            ANSST = ResolveString("ANSST"),
            ANCLC = ResolveString("ANCLC"),
            ANSIDTX = ResolveString("ANSIDTX"),
            ANSIDREVER = ResolveInt("ANSIDREVER")
        };

        // Proc_Contrapartidas reports a debit from CFA. A published mapping cannot
        // silently turn it into a credit, send both amounts, or change its value.
        if (!string.Equals(contract.OFDD, "D", StringComparison.Ordinal)
            || contract.OFMONDEB != debit.Amount
            || contract.OFMONCRE != 0m)
        {
            throw new InvalidOperationException(
                "PROC_CONTRAPARTIDAS_MONETARY_MISMATCH: OFDD/OFMONDEB/OFMONCRE no corresponden al debito de origen.");
        }

        EnsureProtectedResolvedValues(debit, contract.OFCTA, contract.OFIDTX);

        var snapshotHash = await _context.Set<IntegrationMappingSetHistory>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == published.Id)
            .OrderByDescending(x => x.PerformedAtUtc)
            .Select(x => x.SnapshotHash)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return new ProcContrapartidasRequestResolution
        {
            Contract = contract,
            MappingSetId = published.Id,
            MappingVersion = published.Version,
            MappingSnapshotHash = snapshotHash,
            UsedFallback = false
        };

        string ResolveString(string key)
            => resolved.TryGetValue(key, out var v) ? v ?? string.Empty : string.Empty;

        string ResolveRequiredString(string key)
        {
            var value = ResolveString(key);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"INTEGRATION_MAPPING_SOURCE_REQUIRED: {key} no produjo un valor obligatorio.");
        }

        int ResolveInt(string key, int fallback = 0)
            => resolved.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;

        int ResolveRequiredInt(string key)
            => resolved.TryGetValue(key, out var value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : throw new InvalidOperationException($"INTEGRATION_MAPPING_CONVERSION_FAILED: {key} no produjo un entero valido.");

        decimal ResolveRequiredDecimal(string key)
            => resolved.TryGetValue(key, out var value)
                && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : throw new InvalidOperationException($"INTEGRATION_MAPPING_CONVERSION_FAILED: {key} no produjo un decimal valido.");
    }

    // No accredited application source exists yet for these three CFA identifiers.
    // This policy is shared by readiness and runtime; no guessed alternate source is accepted.
    internal static bool IsProtectedParameter(string parameterPath)
        => parameterPath.ToUpperInvariant() is "OFEMP" or "OFIDARCH" or "OFDIRECCIONIP" or "OFCTA" or "OFIDTX";

    internal static string? AssessProtectedRule(
        string parameterPath,
        IntegrationMappingRule rule,
        string? effectiveSourcePath)
    {
        parameterPath = parameterPath.ToUpperInvariant();
        if (parameterPath is "OFEMP" or "OFIDARCH" or "OFDIRECCIONIP")
            return $"PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED: {parameterPath} no tiene fuente funcional acreditada.";

        var expected = parameterPath switch
        {
            "OFCTA" => "transaction.sourceAccountNumber",
            "OFIDTX" => "transaction.reference",
            _ => null
        };
        if (expected is null)
            return null;

        if (rule.SourceKind != IntegrationSourceKindEnum.Transaction
            || !string.Equals(effectiveSourcePath, expected, StringComparison.OrdinalIgnoreCase)
            || rule.FixedValue is not null
            || rule.DefaultValue is not null
            || !string.IsNullOrWhiteSpace(rule.TransformationCode)
            || !string.IsNullOrWhiteSpace(rule.FormatMask)
            || !string.IsNullOrWhiteSpace(rule.ConditionExpression))
            return $"PROC_CONTRAPARTIDAS_PROTECTED_SOURCE_INVALID: {parameterPath} requiere {expected} sin FixedValue, DefaultValue, expresiones ni transformaciones.";

        return null;
    }

    internal static void EnsureProtectedResolvedValues(AchTransaction transaction, string? account, string? reference)
    {
        if (string.IsNullOrWhiteSpace(transaction.SourceAccountNumber)
            || !string.Equals(account, transaction.SourceAccountNumber, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "PROC_CONTRAPARTIDAS_PROTECTED_VALUE_MISMATCH: OFCTA difiere de Transactions.SourceAccountNumber.");
        if (string.IsNullOrWhiteSpace(transaction.Reference)
            || !string.Equals(reference, transaction.Reference, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "PROC_CONTRAPARTIDAS_PROTECTED_VALUE_MISMATCH: OFIDTX difiere de Transactions.Reference.");
    }

    private static RuleResolution ResolveValue(
        IntegrationMappingRule rule,
        IReadOnlyDictionary<long, IntegrationSourceCatalogField> sourceCatalog,
        AchCycle cycle,
        AchTransaction? tx,
        DateTime executionDateTime)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return new RuleResolution(
                ApplyTransformation(rule.FixedValue, rule.TransformationCode, rule.FormatMask),
                UsedDefault: false);
        }

        var sourcePath = !string.IsNullOrWhiteSpace(rule.SourceFieldPath)
            ? rule.SourceFieldPath
            : (rule.SourceCatalogFieldId.HasValue && sourceCatalog.TryGetValue(rule.SourceCatalogFieldId.Value, out var field)
                ? field.FieldPath
                : string.Empty);

        var resolved = rule.SourceKind switch
        {
            IntegrationSourceKindEnum.Constant => rule.DefaultValue,
            IntegrationSourceKindEnum.Expression => null,
            _ => ResolvePath(sourcePath, cycle, tx, executionDateTime)
        };

        var usedDefault = rule.SourceKind != IntegrationSourceKindEnum.Constant
            && resolved is null
            && rule.DefaultValue is not null;
        resolved ??= rule.DefaultValue;
        return new RuleResolution(
            ApplyTransformation(resolved, rule.TransformationCode, rule.FormatMask),
            usedDefault);
    }

    private static string? ResolvePath(string sourcePath, AchCycle cycle, AchTransaction? tx, DateTime executionDateTime)
    {
        var key = sourcePath.Trim().ToLowerInvariant();
        return key switch
        {
            "cycle.id" => cycle.Id,
            "cycle.processingdate" => cycle.ProcessingDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            "clearinghouse.id" => cycle.ClearingHouseId.ToString(CultureInfo.InvariantCulture),
            "clearinghouse.code" => cycle.ClearingHouse?.Code,
            "transaction.id" => tx?.Id.ToString(CultureInfo.InvariantCulture),
            "transaction.transactionexternalid" => tx?.TransactionExternalId,
            "transaction.reference" => tx?.Reference,
            "transaction.amount" => tx?.Amount.ToString(CultureInfo.InvariantCulture),
            "transaction.debitcreditindicator" => tx?.Type switch
            {
                TransactionTypeEnum.Debit => "D",
                TransactionTypeEnum.Credit => "C",
                _ => null
            },
            "transaction.tracenumber" => tx?.TraceNumber,
            "transaction.originatingdfi" => tx?.OriginatingDFI,
            "transaction.companyidentification" => tx?.CompanyIdentification,
            "transaction.sourceaccountnumber" => tx?.SourceAccountNumber,
            "transaction.effectiveentrydate" => tx?.EffectiveEntryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            "batch.id" => tx?.AchBatchId.ToString(CultureInfo.InvariantCulture),
            "execution.datetimeutc" => executionDateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            "execution.dateyyyymmdd" => executionDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            _ => null
        };
    }

    private static string? ApplyTransformation(string? value, string? transformationCode, string? formatMask)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(transformationCode))
        {
            return value;
        }

        return transformationCode switch
        {
            "Trim" => value.Trim(),
            "Uppercase" => value.ToUpperInvariant(),
            "Lowercase" => value.ToLowerInvariant(),
            "PadLeft" when int.TryParse(formatMask, out var left) => value.PadLeft(left, '0'),
            "PadRight" when int.TryParse(formatMask, out var right) => value.PadRight(right, '0'),
            "NullIfEmpty" => string.IsNullOrWhiteSpace(value) ? null : value,
            _ => value
        };
    }

    private readonly record struct RuleResolution(string? Value, bool UsedDefault);
}
