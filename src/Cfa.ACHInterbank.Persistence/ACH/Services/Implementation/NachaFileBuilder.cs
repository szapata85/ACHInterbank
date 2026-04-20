using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Application.Helpers.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFileBuilder : INachaFileBuilder
{
    private static readonly ConcurrentDictionary<string, string> NormalizedIdentifierCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string[]> IdentifierCandidatesCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, PropertyResolutionCache> PropertyCacheByType = new();
    private static readonly ConcurrentDictionary<(Type Type, string Identifier), PropertyLookupResult> PropertyLookupCache = new();

    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;
    private readonly INachaDataLoader _dataLoader;
    private readonly INachaTransactionValidationService _transactionValidationService;
    private readonly INachaFixedWidthRecordRenderer _recordRenderer;
    private readonly INachaRecordDataProvider _recordDataProvider;
    private readonly INachaSemanticValidator _nachaSemanticValidator;
    private readonly IBatchNumberGenerator _batchNumberGenerator;
    private readonly INachaConfigResolver? _configResolver;
    private readonly INachaType7AliasMap? _type7AliasMap;
    private readonly INachaType7GenerationStrategy? _type7GenerationStrategy;
    private readonly INachaType7LegacyRenderer? _type7LegacyRenderer;
    private readonly INachaType7RolloutPolicy? _type7RolloutPolicy;
    private readonly INachaRecordMappingEngine? _recordMappingEngine;
    private readonly IFieldMappingPlanCompiler? _mappingPlanCompiler;
    private readonly NachaGenerationOptions _generationOptions;
    private readonly ILogger<NachaFileBuilder>? _logger;

    public NachaFileBuilder(
        AchDbContext context,
        IBankHoliday holidayService,
        INachaDataLoader dataLoader,
        INachaTransactionValidationService transactionValidationService,
        INachaFixedWidthRecordRenderer recordRenderer,
        INachaRecordDataProvider recordDataProvider,
        INachaSemanticValidator nachaSemanticValidator,
        INachaConfigResolver? configResolver = null,
        INachaType7AliasMap? type7AliasMap = null,
        INachaType7GenerationStrategy? type7GenerationStrategy = null,
        INachaType7LegacyRenderer? type7LegacyRenderer = null,
        INachaType7RolloutPolicy? type7RolloutPolicy = null,
        INachaRecordMappingEngine? recordMappingEngine = null,
        IFieldMappingPlanCompiler? mappingPlanCompiler = null,
        IOptions<NachaGenerationOptions>? generationOptions = null,
        ILogger<NachaFileBuilder>? logger = null,
        IBatchNumberGenerator? batchNumberGenerator = null)
    {
        _context = context;
        _holidayService = holidayService;
        _dataLoader = dataLoader;
        _transactionValidationService = transactionValidationService;
        _recordRenderer = recordRenderer;
        _recordDataProvider = recordDataProvider;
        _nachaSemanticValidator = nachaSemanticValidator;
        _batchNumberGenerator = batchNumberGenerator ?? new DailyResetBatchNumberGenerator(new BatchNumberSequenceStore(_context));
        _configResolver = configResolver;
        _type7AliasMap = type7AliasMap;
        _type7GenerationStrategy = type7GenerationStrategy;
        _type7LegacyRenderer = type7LegacyRenderer;
        _type7RolloutPolicy = type7RolloutPolicy;
        _recordMappingEngine = recordMappingEngine;
        _mappingPlanCompiler = mappingPlanCompiler;
        _generationOptions = generationOptions?.Value ?? new NachaGenerationOptions();
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO PRINCIPAL: Generar archivo NACHA-M por lotes
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildNachaFileAsync(IEnumerable<int> batchIds, CancellationToken ct = default)
    {
        var batches = await _dataLoader.LoadBatchesByIdsAsync(batchIds, ct);

        if (!batches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        var cycle = batches.First().AchCycle!;
        var nachaHeader = await _dataLoader.LoadHeaderAsync(cycle.Id, ct);
        var layoutCache = await _dataLoader.LoadLayoutsAsync(ct);

        var transactions = batches.SelectMany(b => b.Transactions).ToList();
        await _transactionValidationService.ValidateTransactionsForSendAsync(transactions, ct);
        var definitions = await _dataLoader.LoadDefinitionsAsync(ct);
        var context = new NachaBuildContext
        {
            Cycle = cycle,
            Batches = batches,
            Transactions = transactions
        };
        return await BuildFileAsync(context, definitions, layoutCache, nachaHeader, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO ALTERNATIVO: Generar NACHA-M por ciclo
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildNachaFileByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var context = await _dataLoader.LoadByCycleAsync(cycleId, ct);
        var cycle = context.Cycle;
        var transactions = context.Transactions;
        var batches = context.Batches;

        if (transactions.Count == 0)
            throw new InvalidOperationException($"El ciclo {cycleId} no tiene transacciones para exportar.");

        if (batches.Count == 0)
            throw new InvalidOperationException($"El ciclo {cycleId} no tiene lotes asociados para exportar.");

        var nachaHeader = await _dataLoader.LoadHeaderAsync(cycle.Id, ct);
        var layoutCache = await _dataLoader.LoadLayoutsAsync(ct);
        await _transactionValidationService.ValidateTransactionsForSendAsync(transactions, ct);
        var definitions = await _dataLoader.LoadDefinitionsAsync(ct);
        return await BuildFileAsync(context, definitions, layoutCache, nachaHeader, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO DE INTERFAZ: Cumple contrato de INachaFileBuilder
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct = default)
    {
        var layoutCache = await _dataLoader.LoadLayoutsAsync(ct);
        if (!layoutCache.TryGetValue(recordType, out var layout))
        {
            throw new InvalidOperationException($"Layout no encontrado para '{recordType}'.");
        }

        return await _recordRenderer.RenderRecordAsync(recordType, entity, layout);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO INTERNO OPTIMIZADO
    // ─────────────────────────────────────────────────────────────────────────────
    private static Task<string> BuildRecordInternalAsync<T>(string recordType, T entity, NachaRecordLayout layout)
    {
        var fields = layout.Fields.OrderBy(f => f.StartPosition).ToList();
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');

        if (!string.IsNullOrEmpty(layout.RecordCode))
            buffer[0] = layout.RecordCode[0];

        var entityType = entity?.GetType() ?? typeof(T);

        foreach (var field in fields)
        {
            object? raw;
            if (TryResolveConstant(field.DbColumn, out raw))
            {
                string constantValue = FormatValue(raw, field);

                if (constantValue.Length > field.Length)
                    constantValue = constantValue.Substring(0, field.Length);

                constantValue = field.Justification == 'R'
                    ? constantValue.PadLeft(field.Length, field.PadChar)
                    : constantValue.PadRight(field.Length, field.PadChar);

                int constantStart = field.StartPosition - 1;
                constantValue.CopyTo(0, buffer, constantStart, constantValue.Length);
                continue;
            }

            var prop = ResolveProperty(entityType, field.DbColumn)
                ?? ResolveProperty(entityType, field.FieldName);
            if (prop == null) continue;

            raw = prop.GetValue(entity);
            string value = FormatValue(raw, field);

            if (recordType == "5" && string.Equals(field.FieldName, "SettlementDate", StringComparison.OrdinalIgnoreCase))
            {
                value = NormalizeBatchSettlementDate(value);
            }

            if (value.Length > field.Length)
                value = value.Substring(0, field.Length);

            value = field.Justification == 'R'
                ? value.PadLeft(field.Length, field.PadChar)
                : value.PadRight(field.Length, field.PadChar);

            int start = field.StartPosition - 1;
            value.CopyTo(0, buffer, start, value.Length);
        }

        return Task.FromResult(new string(buffer));
    }

    private static Task<string> BuildRecordInternalAsync(
        string recordType,
        IReadOnlyDictionary<string, object?> values,
        NachaRecordLayout layout)
    {
        var fields = layout.Fields.OrderBy(f => f.StartPosition).ToList();
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');

        if (!string.IsNullOrEmpty(layout.RecordCode))
            buffer[0] = layout.RecordCode[0];

        foreach (var field in fields)
        {
            object? raw;
            if (!TryResolveConstant(field.DbColumn, out raw) &&
                !TryResolveValue(values, field.DbColumn, out raw) &&
                !TryResolveValue(values, field.FieldName, out raw))
            {
                continue;
            }

            var value = FormatValue(raw, field);

            if (recordType == "5" && string.Equals(field.FieldName, "SettlementDate", StringComparison.OrdinalIgnoreCase))
            {
                value = NormalizeBatchSettlementDate(value);
            }

            if (value.Length > field.Length)
                value = value.Substring(0, field.Length);

            value = field.Justification == 'R'
                ? value.PadLeft(field.Length, field.PadChar)
                : value.PadRight(field.Length, field.PadChar);

            int start = field.StartPosition - 1;
            value.CopyTo(0, buffer, start, value.Length);
        }

        return Task.FromResult(new string(buffer));
    }

    private static string NormalizeBatchSettlementDate(string? value)
    {
        var validation = BatchHeaderType5JulianDateValidator.ValidateAndFormat(value);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Error Fatal 65 en Fecha de Compensación Juliana.");
        }

        return validation.FormattedValue;
    }

    private static PropertyInfo? ResolveProperty(Type type, string? dbColumn)
    {
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return null;
        }

        var normalizedIdentifier = dbColumn.Trim();
        var lookupResult = PropertyLookupCache.GetOrAdd(
            (type, normalizedIdentifier),
            static key => ResolvePropertyUncached(key.Type, key.Identifier));

        return lookupResult.Found ? lookupResult.Property : null;
    }

    private static PropertyLookupResult ResolvePropertyUncached(Type type, string identifier)
    {
        var typeCache = PropertyCacheByType.GetOrAdd(type, static t =>
        {
            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var normalizedMap = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            foreach (var property in properties)
            {
                var normalizedPropertyName = NormalizeIdentifier(property.Name);
                if (!normalizedMap.ContainsKey(normalizedPropertyName))
                {
                    normalizedMap[normalizedPropertyName] = property;
                }
            }

            return new PropertyResolutionCache(normalizedMap);
        });

        foreach (var candidate in EnumerateIdentifierCandidates(identifier))
        {
            var property = type.GetProperty(candidate,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);
            if (property is not null)
            {
                return PropertyLookupResult.From(property);
            }

            var normalizedTarget = NormalizeIdentifier(candidate);
            if (typeCache.NormalizedProperties.TryGetValue(normalizedTarget, out property))
            {
                return PropertyLookupResult.From(property);
            }
        }

        return PropertyLookupResult.NotFound;
    }

    private static bool TryResolveConstant(string? dbColumn, out object? raw)
    {
        raw = null;
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return false;
        }

        if (!dbColumn.StartsWith("CONST:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        raw = dbColumn[6..];
        return true;
    }

    private static bool TryResolveValue(IReadOnlyDictionary<string, object?> values, string? dbColumn, out object? raw)
    {
        raw = null;
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return false;
        }

        foreach (var candidate in EnumerateIdentifierCandidates(dbColumn))
        {
            if (values.TryGetValue(candidate, out raw))
            {
                return true;
            }

            var exactIgnoreCase = values.FirstOrDefault(kv => string.Equals(kv.Key, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(exactIgnoreCase.Key))
            {
                raw = exactIgnoreCase.Value;
                return true;
            }

            var normalizedTarget = NormalizeIdentifier(candidate);
            var normalizedMatch = values.FirstOrDefault(kv => NormalizeIdentifier(kv.Key) == normalizedTarget);
            if (!string.IsNullOrEmpty(normalizedMatch.Key))
            {
                raw = normalizedMatch.Value;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateIdentifierCandidates(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var candidate in IdentifierCandidatesCache.GetOrAdd(value, static raw =>
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                return Array.Empty<string>();
            }

            var separators = new[] { '.', ':', '/' };
            var segments = trimmed.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length > 1)
            {
                return new[] { trimmed, segments[^1] };
            }

            return new[] { trimmed };
        }))
        {
            yield return candidate;
        }
    }

    private static string NormalizeIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return NormalizedIdentifierCache.GetOrAdd(value, static raw =>
        {
            var chars = new char[raw.Length];
            var index = 0;

            foreach (var ch in raw)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    chars[index++] = char.ToUpperInvariant(ch);
                }
            }

            return index == 0 ? string.Empty : new string(chars, 0, index);
        });
    }

    private sealed record PropertyResolutionCache(IReadOnlyDictionary<string, PropertyInfo> NormalizedProperties);

    private readonly record struct PropertyLookupResult(bool Found, PropertyInfo? Property)
    {
        public static PropertyLookupResult NotFound => new(false, null);
        public static PropertyLookupResult From(PropertyInfo property) => new(true, property);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // AUXILIAR: Formateo de valores
    // ─────────────────────────────────────────────────────────────────────────────
    private static string FormatValue(object? raw, NachaRecordField field)
    {
        if (raw == null) return string.Empty;

        return raw switch
        {
            DateTime dt => dt.ToString(field.Format ?? "yyyyMMdd"),
            decimal d => ((long)(d * 100)).ToString(),
            bool b => b ? "1" : "0",
            _ => raw.ToString() ?? string.Empty
        };
    }

    private async Task<string> BuildFileAsync(
        NachaBuildContext context,
        IReadOnlyList<NachaRecordDefinition> definitions,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaHeader? header,
        CancellationToken ct)
    {
        var orderedBatches = context.Batches.OrderBy(b => b.Id).ToList();
        var clearingHouseCode = context.Cycle.ClearingHouse?.Name?.Contains("CENIT", StringComparison.OrdinalIgnoreCase) == true ? "CENIT" : "ACH";
        var batchNumberAssignment = await _batchNumberGenerator.AssignBatchNumbersAsync(orderedBatches, clearingHouseCode, context.Cycle.ProcessingDate, ct);
        var batchSequenceById = batchNumberAssignment.BatchNumberByBatchId;

        if (!orderedBatches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        var transactionCount = context.Transactions.Count;
        var estimatedRecordCount = Math.Max(1, transactionCount * 3 + (orderedBatches.Count * 4) + 10);
        var estimatedRecordLength = layoutCache.TryGetValue("6", out var entryLayout)
            ? entryLayout.TotalLength
            : 106;
        var sb = new StringBuilder(capacity: estimatedRecordCount * estimatedRecordLength);
        var audit = new NachaGenerationAuditResult
        {
            Mode = (_generationOptions.Mode ?? "LEGACY").Trim().ToUpperInvariant(),
            ClearingHouseCode = clearingHouseCode
        };
        var settlementPolicy = ResolveSettlementPolicyByChamber(audit.ClearingHouseCode);
        audit.Trace.Add($"HeaderPolicy:Chamber={audit.ClearingHouseCode};SettlementDate={settlementPolicy}");
        audit.Trace.Add($"BatchNumberPolicy:{batchNumberAssignment.PolicyCode};ScopedGroups={batchNumberAssignment.ScopedGroups}");
        foreach (var scopeTrace in batchNumberAssignment.ScopeTrace)
        {
            audit.Trace.Add($"BatchNumberScope:{scopeTrace.Scope};Policy={scopeTrace.PolicyCode};Previous={scopeTrace.PreviousValue};Assigned={scopeTrace.AssignedValue};WasCreated={scopeTrace.WasCreated};Reserved={scopeTrace.ReservedCount}");
        }

        var resolution = await ResolveRuntimeConfigAsync(context, definitions, ct);
        if (resolution.Profile is not null)
        {
            audit.ProfileId = resolution.Profile.Id;
            audit.ProfileCode = resolution.Profile.ProfileCode;
            audit.Trace.AddRange(resolution.Trace);
            audit.Warnings.AddRange(resolution.Warnings);
        }

        var transactionsByBatchId = new Dictionary<int, List<AchTransaction>>(orderedBatches.Count);
        foreach (var tx in context.Transactions)
        {
            if (!transactionsByBatchId.TryGetValue(tx.AchBatchId, out var list))
            {
                list = new List<AchTransaction>();
                transactionsByBatchId[tx.AchBatchId] = list;
            }

            list.Add(tx);
        }

        foreach (var list in transactionsByBatchId.Values)
        {
            list.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        }

        long totalDebit = 0, totalCredit = 0;
        int recordCount = 0, batchCount = orderedBatches.Count, entryAddendaCount = 0;

        var companyEntryDescriptionCatalog = (await _dataLoader.LoadCompanyEntryDescriptionCatalogAsync(ct))
            .Select(item => new CompanyEntryDescriptionCatalogItem(item.Term, item.StandardEntryClassCode))
            .ToList();

        var batchCalculations = new Dictionary<int, BatchCalculation>(orderedBatches.Count);
        var totalAddendaOnlyCount = 0;
        foreach (var batch in orderedBatches)
        {
            var batchTransactions = transactionsByBatchId.TryGetValue(batch.Id, out var txs)
                ? (IReadOnlyList<AchTransaction>)txs
                : Array.Empty<AchTransaction>();

            var addendaCount = 0;
            long batchDebit = 0, batchCredit = 0;
            var creditLikeCount = 0;

            foreach (var tx in batchTransactions)
            {
                if (tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
                {
                    creditLikeCount++;
                    batchCredit += (long)(tx.Amount * 100);
                }
                else
                {
                    batchDebit += (long)(tx.Amount * 100);
                }

                addendaCount += tx.Addendas is { Count: > 0 } ? tx.Addendas.Count : 1;
            }

            totalDebit += batchDebit;
            totalCredit += batchCredit;
            totalAddendaOnlyCount += addendaCount;

            var description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
            var secCode = ResolveStandardEntryClassCode(batch, batchTransactions, companyEntryDescriptionCatalog);
            var batchDescription = creditLikeCount > 1 ? "MULTICREDIT" : description;

            batchCalculations[batch.Id] = new BatchCalculation(
                Transactions: batchTransactions,
                EntryAddendaCount: batchTransactions.Count + addendaCount,
                AddendaOnlyCount: addendaCount,
                BatchDebit: batchDebit,
                BatchCredit: batchCredit,
                StandardEntryClassCode: secCode,
                BatchEntryDescription: batchDescription);
        }

        foreach (var definition in definitions)
        {
            if (!definition.IsEnabled)
            {
                continue;
            }

            switch (definition.RecordCode)
            {
                case "1":
                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        [FileHeaderRecord.From(context.Cycle, context.Transactions, header)],
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);
                    break;
                case "5":
                    var type5Records = new List<object>(orderedBatches.Count);
                    foreach (var batch in orderedBatches)
                    {
                        var calculation = batchCalculations[batch.Id];
                        type5Records.Add(BatchHeaderRecord.From(
                            batch,
                            calculation.StandardEntryClassCode,
                            batchSequenceById[batch.Id],
                            calculation.BatchEntryDescription));
                    }

                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        type5Records,
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);
                    break;
                case "6":
                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        await BuildEntryDetailRecordsAsync(context.Transactions, ct),
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);
                    entryAddendaCount += transactionCount;
                    break;
                case "7":
                    var type7Candidates = (_type7GenerationStrategy?.BuildCandidates(orderedBatches)
                                           ?? BuildFallbackType7Candidates(orderedBatches)).ToList();
                    recordCount += await AppendType7ResolvedOrLegacyAsync(
                        sb,
                        definition,
                        layoutCache,
                        resolution,
                        audit,
                        type7Candidates,
                        context,
                        ct);
                    entryAddendaCount += type7Candidates.Count;
                    break;
                case "8":
                    var type8Records = new List<object>(orderedBatches.Count);
                    foreach (var batch in orderedBatches)
                    {
                        var calculation = batchCalculations[batch.Id];
                        type8Records.Add(BatchControlRecord.From(
                            batch,
                            calculation.EntryAddendaCount,
                            calculation.BatchDebit,
                            calculation.BatchCredit,
                            batchSequenceById[batch.Id]));
                    }

                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        type8Records,
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);
                    break;
                case "9":
                    var totalRecords = recordCount + 1;
                    var blockCount = (int)Math.Ceiling(totalRecords / 10m);
                    var paddingNeeded = (blockCount * 10) - totalRecords;
                    var fileControl = FileControlRecord.From(context.Cycle, orderedBatches, batchCount, blockCount, entryAddendaCount, totalDebit, totalCredit);
                    audit.Trace.Add($"FileIntegrity:BatchCount={batchCount};EntryAddendaCount={entryAddendaCount};TotalDebit={totalDebit};TotalCredit={totalCredit};BlockCount={blockCount};PaddingNeeded={paddingNeeded}");

                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        [fileControl],
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);

                    if (paddingNeeded > 0)
                    {
                        var paddingRecord = new string('9', layoutCache["9"].TotalLength);
                        EnsureRecordLength("9", paddingRecord, layoutCache["9"].TotalLength);
                        for (int i = 0; i < paddingNeeded; i++)
                        {
                            sb.Append(paddingRecord);
                        }
                    }
                    break;
            }
        }

        var fileContent = sb.ToString();
        await PersistGenerationAuditAsync(audit, resolution.Profile?.Id, ct);
        if (audit.Warnings.Count > 0)
        {
            _logger?.LogWarning("NACHA generación con advertencias: {Warnings}", string.Join(" | ", audit.Warnings));
        }
        _nachaSemanticValidator.Validate(fileContent, context);
        return fileContent;
    }

    private async Task<int> AppendCustomOrConfiguredAsync(
        StringBuilder sb,
        NachaRecordDefinition definition,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        IEnumerable<object> fallbackRecords,
        NachaBuildContext context,
        CancellationToken ct)
    {
        var layout = layoutCache[definition.RecordCode];
        var fallbackList = fallbackRecords.ToList();

        var forceFallback = definition.RecordCode is "1" or "5" or "8" or "9";
        IReadOnlyList<object> records = (definition.SourceType == NachaRecordSourceType.Custom || forceFallback)
            ? fallbackList
            : await _recordDataProvider.GetRecordsAsync(definition, context, ct);

        if (records.Count == 0 && fallbackList.Count > 0)
        {
            records = fallbackList;
        }

        var count = 0;
        foreach (var record in records)
        {
            count++;
            if (record is IReadOnlyDictionary<string, object?> dict)
            {
                sb.Append(await _recordRenderer.RenderRecordAsync(definition.RecordCode, dict, layout));
            }
            else
            {
                sb.Append(await _recordRenderer.RenderRecordAsync(definition.RecordCode, record, layout));
            }
        }

        return count;
    }

    private async Task<int> AppendResolvedOrLegacyAsync(
        StringBuilder sb,
        string recordCode,
        IEnumerable<object> records,
        NachaRecordDefinition definition,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaConfigResolutionResult resolution,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        var mode = (_generationOptions.Mode ?? "LEGACY").Trim().ToUpperInvariant();
        var settlementPolicy = ResolveSettlementPolicyByChamber(audit.ClearingHouseCode);
        var shouldUseResolver = mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";

        if (!shouldUseResolver || _configResolver is null || !resolution.LayoutsByRecordCode.TryGetValue(recordCode, out var layoutVariant))
        {
            if (!audit.LegacyRecordCodes.Contains(recordCode))
            {
                audit.LegacyRecordCodes.Add(recordCode);
            }

            audit.Warnings.Add($"Fallback legado para RecordCode={recordCode}: no hay layout resuelto o modo legado.");
            return await AppendCustomOrConfiguredAsync(sb, definition, layoutCache, records, context, ct);
        }

        var lineCount = 0;
        foreach (var record in records)
        {
            if (recordCode == "1" && ShouldUseRecord1MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord1WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    audit.Trace.Add("R1:MAPPING_ENGINE_APPLIED:BASE_OBJECT=FileHeaderRecord.From");
                    audit.Trace.Add($"R1:CHAMBER={audit.ClearingHouseCode};SETTLEMENT_POLICY={settlementPolicy}");
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=1 usó fallback legado por cobertura insuficiente del mapping engine.");
                audit.Trace.Add("R1:MAPPING_ENGINE_FALLBACK:BASE_OBJECT=FileHeaderRecord.From");
                audit.Trace.Add($"R1:CHAMBER={audit.ClearingHouseCode};SETTLEMENT_POLICY={settlementPolicy}");
            }

            if (recordCode == "5" && ShouldUseRecord5MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord5WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    audit.Trace.Add("R5:MAPPING_ENGINE_APPLIED:BASE_OBJECT=BatchHeaderRecord.From");
                    audit.Trace.Add($"R5:CHAMBER={audit.ClearingHouseCode};SETTLEMENT_POLICY={settlementPolicy}");
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=5 usó fallback legado por cobertura insuficiente del mapping engine.");
                audit.Trace.Add("R5:MAPPING_ENGINE_FALLBACK:BASE_OBJECT=BatchHeaderRecord.From");
                audit.Trace.Add($"R5:CHAMBER={audit.ClearingHouseCode};SETTLEMENT_POLICY={settlementPolicy}");
            }

            if (recordCode == "6" && ShouldUseRecord6MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord6WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=6 usó fallback legado por cobertura insuficiente del mapping engine.");
            }

            if (recordCode == "8" && ShouldUseRecord8MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord8WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    audit.Trace.Add("R8:MAPPING_ENGINE_APPLIED:BASE_OBJECT=BatchControlRecord.From");
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=8 usó fallback legado por cobertura insuficiente del mapping engine.");
                audit.Trace.Add("R8:MAPPING_ENGINE_FALLBACK:BASE_OBJECT=BatchControlRecord.From");
            }

            if (recordCode == "9" && ShouldUseRecord9MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord9WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    audit.Trace.Add("R9:MAPPING_ENGINE_APPLIED:BASE_OBJECT=FileControlRecord.From");
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=9 usó fallback legado por cobertura insuficiente del mapping engine.");
                audit.Trace.Add("R9:MAPPING_ENGINE_FALLBACK:BASE_OBJECT=FileControlRecord.From");
            }

            var rendered = await RenderWithResolvedLayoutAsync(recordCode, record, layoutVariant);
            if (mode == "SHADOW_COMPARE" && layoutCache.TryGetValue(recordCode, out var legacyLayout))
            {
                var legacyRendered = record is IReadOnlyDictionary<string, object?> shadowDict
                    ? await _recordRenderer.RenderRecordAsync(recordCode, shadowDict, legacyLayout)
                    : await _recordRenderer.RenderRecordAsync(recordCode, record, legacyLayout);

                var diff = CompareRenderedLines(recordCode, legacyRendered, rendered);
                if (!string.IsNullOrWhiteSpace(diff))
                {
                    audit.EquivalenceDiffs.Add(diff);
                    audit.Warnings.Add($"Diferencia detectada en shadow compare para RecordCode={recordCode}.");
                    RegisterShadowDiff(audit, diff);
                }
            }

            EnsureRecordLength(recordCode, rendered, layoutVariant.TotalLength);
            sb.Append(rendered);
            lineCount++;
        }

        if (!audit.NewEngineRecordCodes.Contains(recordCode))
        {
            audit.NewEngineRecordCodes.Add(recordCode);
        }

        return lineCount;
    }

    private async Task<int> AppendType7ResolvedOrLegacyAsync(
        StringBuilder sb,
        NachaRecordDefinition definition,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaConfigResolutionResult resolution,
        NachaGenerationAuditResult audit,
        IReadOnlyList<NachaType7RecordCandidate> candidates,
        NachaBuildContext context,
        CancellationToken ct)
    {
        var mode = (_generationOptions.Mode ?? "LEGACY").Trim().ToUpperInvariant();
        var hasLayout = resolution.LayoutsByRecordCode.TryGetValue("7", out var layoutVariant);
        audit.Type7LayoutVariantCode = layoutVariant?.VariantCode;
        audit.Type7TotalCandidates += candidates.Count;
        var shouldUseTableDriven = _generationOptions.EnableType7TableDriven
                                   && mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE"
                                   && _configResolver is not null
                                   && hasLayout;

        var forcedTableDrivenByLayout = layoutVariant is not null &&
                                        _generationOptions.Type7DisableLegacyFallbackForLayouts
                                            .Any(x => string.Equals(x, layoutVariant.VariantCode, StringComparison.OrdinalIgnoreCase));

        if (_generationOptions.Type7EnableTableDrivenForClearingHouses.Count > 0)
        {
            var clearingHouseName = context.Cycle.ClearingHouse?.Name ?? "ACH";
            var allowForClearingHouse = _generationOptions.Type7EnableTableDrivenForClearingHouses
                .Any(x => clearingHouseName.Contains(x, StringComparison.OrdinalIgnoreCase));
            shouldUseTableDriven &= allowForClearingHouse;
            if (!allowForClearingHouse)
            {
                IncrementCounter(audit.Type7FallbackReasons, $"ClearingHouseNotEnabled:{clearingHouseName}");
            }
        }

        var rolloutDecision = _type7RolloutPolicy is null
            ? new NachaType7RolloutDecision { AllowLegacyFallback = true, Reasons = ["RolloutPolicyNotConfigured"] }
            : await _type7RolloutPolicy.EvaluateAsync(audit.ClearingHouseCode ?? "ACH", layoutVariant, mode, ct);

        audit.Trace.Add($"Type7RolloutDecision:Eligible={rolloutDecision.EligibleToDisableFallback};AllowFallback={rolloutDecision.AllowLegacyFallback};Runs={rolloutDecision.QualifiedRuns};Equivalence={rolloutDecision.EquivalenceRatePercent:0.00};Reasons={string.Join(',', rolloutDecision.Reasons)}");

        if (!shouldUseTableDriven || layoutVariant is null)
        {
            if (!audit.LegacyRecordCodes.Contains("7"))
            {
                audit.LegacyRecordCodes.Add("7");
            }

            audit.Warnings.Add("Fallback legado para RecordCode=7 por cobertura/configuración insuficiente.");
            IncrementCounter(audit.Type7FallbackReasons, "NoLayoutOrModeDisabled");
            IncrementCounter(audit.Type7FallbackByLayout, layoutVariant?.VariantCode ?? "NO_LAYOUT");
            audit.Type7GeneratedLegacy += candidates.Count;
            if (!rolloutDecision.AllowLegacyFallback)
            {
                throw new InvalidOperationException($"Rollout policy bloqueó fallback type7. Razones: {string.Join(", ", rolloutDecision.Reasons)}");
            }
            return AppendType7Legacy(sb, candidates);
        }

        var lineCount = 0;
        foreach (var candidate in candidates)
        {
            var alignedValues = AlignType7ValuesWithLayout(layoutVariant, candidate.FieldValues, audit);
            var rendered = await TryRenderType7WithMappingEngineAsync(layoutVariant, alignedValues, mode, layoutCache, candidate, audit, ct);
            if (string.IsNullOrWhiteSpace(rendered))
            {
                var renderer = _type7LegacyRenderer ?? new NachaType7LegacyRenderer();
                if (!rolloutDecision.AllowLegacyFallback)
                {
                    throw new InvalidOperationException($"Rollout policy bloqueó fallback type7 para candidato Trace={candidate.Transaction.TraceNumber}.");
                }

                rendered = renderer.Render(candidate.Batch, candidate.Transaction, candidate.Addenda);
                audit.Type7GeneratedLegacy++;
                IncrementCounter(audit.Type7FallbackReasons, "MappingEngineFallbackToLegacy");
                IncrementCounter(audit.Type7FallbackByLayout, layoutVariant.VariantCode);
            }
            else
            {
                audit.Type7GeneratedTableDriven++;
            }

            EnsureRecordLength("7", rendered, layoutVariant.TotalLength);
            sb.Append(rendered);
            lineCount++;

            if (mode == "SHADOW_COMPARE")
            {
                var legacy = (_type7LegacyRenderer ?? new NachaType7LegacyRenderer())
                    .Render(candidate.Batch, candidate.Transaction, candidate.Addenda);
                var diff = CompareRenderedLines("7", legacy, rendered);
                if (!string.IsNullOrWhiteSpace(diff))
                {
                    audit.EquivalenceDiffs.Add(diff);
                    audit.Warnings.Add("Diferencia detectada en SHADOW_COMPARE para RecordCode=7.");
                    RegisterShadowDiff(audit, diff);
                }

                foreach (var fieldDiff in BuildType7FieldDiffs(layoutVariant, legacy, rendered))
                {
                    audit.EquivalenceDiffs.Add(fieldDiff);
                    IncrementCounter(audit.Type7DiffByField, fieldDiff.Split(':')[0]);
                }
            }
        }

        if ((forcedTableDrivenByLayout || !rolloutDecision.AllowLegacyFallback) && audit.Type7GeneratedLegacy > 0)
        {
            throw new InvalidOperationException($"Fallback legado deshabilitado para layout {layoutVariant.VariantCode}.");
        }

        if (!audit.NewEngineRecordCodes.Contains("7"))
        {
            audit.NewEngineRecordCodes.Add("7");
        }

        return lineCount;
    }

    private async Task<string?> TryRenderType7WithMappingEngineAsync(
        CfgLayoutVariant layoutVariant,
        IReadOnlyDictionary<string, object?> alignedValues,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaType7RecordCandidate candidate,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        if (!_generationOptions.EnableType7CommonMappingEngine || _recordMappingEngine is null || _mappingPlanCompiler is null)
        {
            return await RenderWithResolvedLayoutAsync("7", alignedValues, layoutVariant);
        }

        var issues = new List<string>();
        var plan = _mappingPlanCompiler.CompileRecordPlan(layoutVariant, issues);
        if (issues.Count > 0)
        {
            audit.Warnings.AddRange(issues.Select(x => $"Type7PlanIssue:{x}"));
        }

        var mapped = await _recordMappingEngine.MapRecordAsync(new RecordMappingRequest
        {
            RecordCode = "7",
            SourceRecord = alignedValues,
            RecordPlan = plan,
            ContextValues = new Dictionary<string, object?>(alignedValues, StringComparer.OrdinalIgnoreCase),
            EnableDiagnostics = _generationOptions.Record6MappingDiagnostics,
            ShadowCompare = string.Equals(mode, "SHADOW_COMPARE", StringComparison.OrdinalIgnoreCase),
            LegacyLayout = layoutCache.TryGetValue("7", out var legacyLayout) ? legacyLayout : null
        }, ct);

        foreach (var trace in mapped.FieldTraces.Take(_generationOptions.Record6MappingDiagnostics ? mapped.FieldTraces.Count : 5))
        {
            audit.Trace.Add($"R7:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:RAW={trace.RawValue}:FINAL={trace.FinalValue}:FB={trace.FallbackStrategy}");
        }

        if (!mapped.Success && mapped.ValuesByFieldCode.Count == 0)
        {
            audit.Warnings.Add($"Type7 mapping engine devolvió fallback para Trace={candidate.Transaction.TraceNumber}.");
            return null;
        }

        var mappedLayout = new NachaRecordLayout
        {
            RecordCode = "7",
            TotalLength = layoutVariant.TotalLength,
            Fields = layoutVariant.Fields
                .OrderBy(x => x.StartPosition)
                .Select(x => new NachaRecordField
                {
                    FieldName = x.FieldCode,
                    DbColumn = x.FieldCode,
                    StartPosition = x.StartPosition,
                    Length = x.Length,
                    Justification = x.Justification,
                    PadChar = x.PadChar,
                    Format = x.FormatMask
                })
                .ToList()
        };

        return await _recordRenderer.RenderRecordAsync("7", mapped.ValuesByFieldCode, mappedLayout);
    }

    private IReadOnlyDictionary<string, object?> AlignType7ValuesWithLayout(
        CfgLayoutVariant layoutVariant,
        IReadOnlyDictionary<string, object?> values,
        NachaGenerationAuditResult audit)
    {
        if (_type7AliasMap is null)
        {
            return values;
        }

        var aligned = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        foreach (var field in layoutVariant.Fields.Where(f => f.IsEnabled))
        {
            var rawPath = field.SourceDefinition.PropertyPath;
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var canonical = _type7AliasMap.GetCanonicalKey(rawPath);
            if (aligned.TryGetValue(canonical, out var value))
            {
                aligned[rawPath] = value;
                aligned[field.FieldCode] = value;
                audit.Type7AliasResolutionTrace.Add($"{field.FieldCode}:{rawPath}->{canonical}");
                continue;
            }

            if (!aligned.ContainsKey(rawPath))
            {
                IncrementCounter(audit.Type7FallbackReasons, $"MissingField:{field.FieldCode}");
            }
        }

        return aligned;
    }

    private static IEnumerable<string> BuildType7FieldDiffs(CfgLayoutVariant layoutVariant, string legacy, string generated)
    {
        foreach (var field in layoutVariant.Fields.Where(x => x.IsEnabled).OrderBy(x => x.StartPosition))
        {
            var start = field.StartPosition - 1;
            if (start < 0 || start + field.Length > legacy.Length || start + field.Length > generated.Length)
            {
                yield return $"{field.FieldCode}:LONGITUD:{legacy.Length}->{generated.Length}";
                continue;
            }

            var legacyValue = legacy.Substring(start, field.Length);
            var newValue = generated.Substring(start, field.Length);
            if (string.Equals(legacyValue, newValue, StringComparison.Ordinal))
            {
                continue;
            }

            var classification = ClassifyFieldDifference(legacyValue, newValue);
            yield return $"{field.FieldCode}:{classification}:LEGACY='{legacyValue}'|NEW='{newValue}'";
        }
    }

    private static string ClassifyFieldDifference(string legacyValue, string newValue)
    {
        if (legacyValue.Trim() == newValue.Trim())
        {
            return "PADDING";
        }

        if (legacyValue.Length != newValue.Length)
        {
            return "LONGITUD";
        }

        if (legacyValue.All(char.IsDigit) && newValue.All(char.IsDigit))
        {
            return "FORMATO";
        }

        return "VALOR";
    }

    private static void IncrementCounter(Dictionary<string, int> map, string key)
    {
        map[key] = map.TryGetValue(key, out var current) ? current + 1 : 1;
    }

    private int AppendType7Legacy(StringBuilder sb, IReadOnlyList<NachaType7RecordCandidate> candidates)
    {
        var renderer = _type7LegacyRenderer ?? new NachaType7LegacyRenderer();
        var count = 0;
        foreach (var candidate in candidates)
        {
            sb.Append(renderer.Render(candidate.Batch, candidate.Transaction, candidate.Addenda));
            count++;
        }

        return count;
    }

    private IReadOnlyList<NachaType7RecordCandidate> BuildFallbackType7Candidates(IReadOnlyList<AchBatch> orderedBatches)
    {
        var fallbackStrategy = new NachaType7GenerationStrategy(new NachaType7FieldValueResolver(new NachaType7AliasMap()));
        return fallbackStrategy.BuildCandidates(orderedBatches);
    }

    private async Task<string> RenderWithResolvedLayoutAsync(string recordCode, object record, CfgLayoutVariant layoutVariant)
    {
        var mappedLayout = new NachaRecordLayout
        {
            RecordCode = recordCode,
            TotalLength = layoutVariant.TotalLength,
            Fields = layoutVariant.Fields
                .OrderBy(x => x.StartPosition)
                .Select(x => new NachaRecordField
                {
                    FieldName = x.FieldCode,
                    DbColumn = ResolveDbColumnAlias(x.SourceDefinition)!,
                    StartPosition = x.StartPosition,
                    Length = x.Length,
                    Justification = x.Justification,
                    PadChar = x.PadChar,
                    Format = x.FormatMask
                })
                .ToList()
        };

        if (record is IReadOnlyDictionary<string, object?> dict)
        {
            return await _recordRenderer.RenderRecordAsync(recordCode, dict, mappedLayout);
        }

        return await _recordRenderer.RenderRecordAsync(recordCode, record, mappedLayout);
    }

    private static string? ResolveDbColumnAlias(CfgFieldSourceDefinition source)
    {
        if (source.DataSourceType.Code == "CONSTANTE")
        {
            return $"CONST:{source.ConstantValue}";
        }

        return source.PropertyPath;
    }

    private async Task<NachaConfigResolutionResult> ResolveRuntimeConfigAsync(
        NachaBuildContext context,
        IReadOnlyList<NachaRecordDefinition> definitions,
        CancellationToken ct)
    {
        if (_configResolver is null)
        {
            return new NachaConfigResolutionResult
            {
                Success = false,
                UsedFallback = true,
                Warnings = ["Resolver de configuración NACHA no está registrado."],
                Trace = []
            };
        }

        var flow = ResolveFlowCode(context.Transactions);
        var direction = ResolveDirectionCode(context.Transactions);
        var serviceClassCode = context.Batches
            .Select(x => x.ServiceClassCode)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var request = new NachaConfigResolutionRequest
        {
            ClearingHouseCode = context.Cycle.ClearingHouse?.Name?.Contains("CENIT", StringComparison.OrdinalIgnoreCase) == true ? "CENIT" : "ACH",
            FlowTypeCode = flow,
            DirectionCode = direction,
            ServiceClassCode = serviceClassCode,
            ProcessDateUtc = context.Cycle.ProcessingDate,
            RecordCodes = definitions.Where(x => x.IsEnabled).Select(x => x.RecordCode).ToList(),
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CycleName"] = context.Cycle.CycleName ?? string.Empty,
                ["ClearingHouseId"] = context.Cycle.ClearingHouseId.ToString()
            }
        };

        var resolution = await _configResolver.ResolveAsync(request, ct);
        if (_generationOptions.FailOnResolverAmbiguity && resolution.Warnings.Any(x => x.Contains("Ambigüedad", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Resolver NACHA encontró ambigüedad: {string.Join(" | ", resolution.Warnings)}");
        }

        return resolution;
    }

    private static string ResolveFlowCode(IReadOnlyList<AchTransaction> transactions)
    {
        if (transactions.Any(x => x.Type is TransactionTypeEnum.Return or TransactionTypeEnum.Reversal))
        {
            return "RETORNO";
        }

        if (transactions.Any(x => x.Type == TransactionTypeEnum.Prenotification))
        {
            return "PRENOTIFICACION";
        }

        return "ORIGINAL";
    }

    private static string ResolveDirectionCode(IReadOnlyList<AchTransaction> transactions)
    {
        return transactions.Any(x => x.Type is TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
            ? "ENTRADA"
            : "SALIDA";
    }

    private async Task<string?> TryRenderRecord6WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        if (_recordMappingEngine is null || _mappingPlanCompiler is null)
        {
            return null;
        }

        var issues = new List<string>();
        var plan = _mappingPlanCompiler.CompileRecordPlan(layoutVariant, issues);
        if (issues.Count > 0)
        {
            audit.Warnings.AddRange(issues.Select(x => $"Record6PlanIssue:{x}"));
        }

        var contextValues = BuildRecord6ContextValues(record);
        var mapped = await _recordMappingEngine.MapRecordAsync(new RecordMappingRequest
        {
            RecordCode = recordCode,
            SourceRecord = record,
            RecordPlan = plan,
            ContextValues = contextValues,
            EnableDiagnostics = _generationOptions.Record6MappingDiagnostics,
            ShadowCompare = string.Equals(mode, "SHADOW_COMPARE", StringComparison.OrdinalIgnoreCase),
            LegacyLayout = layoutCache.TryGetValue(recordCode, out var legacyLayout) ? legacyLayout : null
        }, ct);

        foreach (var trace in mapped.FieldTraces.Take(_generationOptions.Record6MappingDiagnostics ? mapped.FieldTraces.Count : 5))
        {
            var transforms = trace.TransformSteps.Count == 0 ? "none" : string.Join(",", trace.TransformSteps);
            var issuesText = trace.ValidationIssues.Count == 0
                ? "none"
                : string.Join("|", trace.ValidationIssues.Select(x => $"{x.RuleCode}:{x.Severity}"));
            audit.Trace.Add($"R6:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:RAW={trace.RawValue}:TF={transforms}:RULES={issuesText}:FB={trace.FallbackStrategy}:FINAL={trace.FinalValue}");
        }

        if (!mapped.Success && mapped.ValuesByFieldCode.Count == 0)
        {
            return null;
        }

        var mappedLayout = new NachaRecordLayout
        {
            RecordCode = recordCode,
            TotalLength = layoutVariant.TotalLength,
            Fields = layoutVariant.Fields
                .OrderBy(x => x.StartPosition)
                .Select(x => new NachaRecordField
                {
                    FieldName = x.FieldCode,
                    DbColumn = x.FieldCode,
                    StartPosition = x.StartPosition,
                    Length = x.Length,
                    Justification = x.Justification,
                    PadChar = x.PadChar,
                    Format = x.FormatMask
                })
                .ToList()
        };

        var rendered = await _recordRenderer.RenderRecordAsync(recordCode, mapped.ValuesByFieldCode, mappedLayout);
        if (mode == "SHADOW_COMPARE" && layoutCache.TryGetValue(recordCode, out var legacy))
        {
            var legacyRendered = await _recordRenderer.RenderRecordAsync(recordCode, record, legacy);
            var diff = CompareRenderedLines(recordCode, legacyRendered, rendered);
            if (!string.IsNullOrWhiteSpace(diff))
            {
                audit.EquivalenceDiffs.Add($"R6_MAPPING:{diff}");
                audit.Warnings.Add("Diferencia SHADOW_COMPARE en record 6 mapping engine.");
                RegisterShadowDiff(audit, $"R6_MAPPING:{diff}");
            }
        }

        EnsureRecordLength(recordCode, rendered, mappedLayout.TotalLength);
        return rendered;
    }

    private Task<string?> TryRenderRecord1WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        return TryRenderHeaderWithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
    }

    private Task<string?> TryRenderRecord5WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        return TryRenderHeaderWithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
    }

    private Task<string?> TryRenderRecord8WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        return TryRenderHeaderWithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
    }

    private Task<string?> TryRenderRecord9WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        return TryRenderHeaderWithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
    }

    private async Task<string?> TryRenderHeaderWithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        if (_recordMappingEngine is null || _mappingPlanCompiler is null)
        {
            return null;
        }

        var issues = new List<string>();
        var plan = _mappingPlanCompiler.CompileRecordPlan(layoutVariant, issues);
        if (issues.Count > 0)
        {
            audit.Warnings.AddRange(issues.Select(x => $"Record{recordCode}PlanIssue:{x}"));
        }

        var contextValues = BuildHeaderContextValues(record, context);
        var mapped = await _recordMappingEngine.MapRecordAsync(new RecordMappingRequest
        {
            RecordCode = recordCode,
            SourceRecord = record,
            RecordPlan = plan,
            ContextValues = contextValues,
            EnableDiagnostics = _generationOptions.Record6MappingDiagnostics,
            ShadowCompare = string.Equals(mode, "SHADOW_COMPARE", StringComparison.OrdinalIgnoreCase),
            LegacyLayout = layoutCache.TryGetValue(recordCode, out var legacyLayout) ? legacyLayout : null
        }, ct);

        foreach (var trace in mapped.FieldTraces.Take(_generationOptions.Record6MappingDiagnostics ? mapped.FieldTraces.Count : 5))
        {
            var transforms = trace.TransformSteps.Count == 0 ? "none" : string.Join(",", trace.TransformSteps);
            audit.Trace.Add($"R{recordCode}:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:RAW={trace.RawValue}:TF={transforms}:FB={trace.FallbackStrategy}:FINAL={trace.FinalValue}");
        }

        if (!mapped.Success && mapped.ValuesByFieldCode.Count == 0)
        {
            return null;
        }

        var mappedLayout = new NachaRecordLayout
        {
            RecordCode = recordCode,
            TotalLength = layoutVariant.TotalLength,
            Fields = layoutVariant.Fields
                .OrderBy(x => x.StartPosition)
                .Select(x => new NachaRecordField
                {
                    FieldName = x.FieldCode,
                    DbColumn = x.FieldCode,
                    StartPosition = x.StartPosition,
                    Length = x.Length,
                    Justification = x.Justification,
                    PadChar = x.PadChar,
                    Format = x.FormatMask
                })
                .ToList()
        };

        var rendered = await _recordRenderer.RenderRecordAsync(recordCode, mapped.ValuesByFieldCode, mappedLayout);
        if (mode == "SHADOW_COMPARE" && layoutCache.TryGetValue(recordCode, out var legacy))
        {
            var legacyRendered = await _recordRenderer.RenderRecordAsync(recordCode, record, legacy);
            var diff = CompareRenderedLines(recordCode, legacyRendered, rendered);
            if (!string.IsNullOrWhiteSpace(diff))
            {
                audit.EquivalenceDiffs.Add($"R{recordCode}_MAPPING:{diff}");
                audit.Warnings.Add($"Diferencia SHADOW_COMPARE en record {recordCode} mapping engine.");
                RegisterShadowDiff(audit, $"R{recordCode}_MAPPING:{diff}");
            }
        }

        EnsureRecordLength(recordCode, rendered, mappedLayout.TotalLength);
        return rendered;
    }

    private bool ShouldUseRecord6MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord6MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private bool ShouldUseRecord1MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord1MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private bool ShouldUseRecord5MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord5MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private bool ShouldUseRecord8MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord8MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private bool ShouldUseRecord9MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord9MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private static IReadOnlyDictionary<string, object?> BuildRecord6ContextValues(object record)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var type = record.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            map[property.Name] = property.GetValue(record);
            map[$"ctx.{property.Name}"] = property.GetValue(record);
        }

        return map;
    }

    private static IReadOnlyDictionary<string, object?> BuildHeaderContextValues(object record, NachaBuildContext context)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["CycleName"] = context.Cycle.CycleName,
            ["ProcessingDateUtc"] = context.Cycle.ProcessingDate,
            ["ClearingHouseName"] = context.Cycle.ClearingHouse?.Name,
            ["ClearingHouseId"] = context.Cycle.ClearingHouseId,
            ["TransactionCount"] = context.Transactions.Count
        };

        var type = record.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = property.GetValue(record);
            map[property.Name] = value;
            map[$"ctx.{property.Name}"] = value;
        }

        return map;
    }

    private static string? CompareRenderedLines(string recordCode, string legacyRendered, string newRendered)
    {
        if (string.Equals(legacyRendered, newRendered, StringComparison.Ordinal))
        {
            return null;
        }

        var max = Math.Min(legacyRendered.Length, newRendered.Length);
        var diffs = new List<string>(capacity: 3);
        var totalDiffs = 0;
        for (var i = 0; i < max; i++)
        {
            if (legacyRendered[i] != newRendered[i])
            {
                totalDiffs++;
                if (diffs.Count < 3)
                {
                    diffs.Add($"pos={i + 1},legacy='{legacyRendered[i]}',new='{newRendered[i]}'");
                }
            }
        }

        totalDiffs += Math.Abs(legacyRendered.Length - newRendered.Length);

        if (diffs.Count > 0)
        {
            return $"RecordCode={recordCode};DiffCount={totalDiffs};Diffs={string.Join("|", diffs)};LenLegacy={legacyRendered.Length};LenNew={newRendered.Length}";
        }

        return $"RecordCode={recordCode}, longitud legado={legacyRendered.Length}, longitud nuevo={newRendered.Length}";
    }

    private static void RegisterShadowDiff(NachaGenerationAuditResult audit, string diff)
    {
        audit.ShadowDiffCount++;
        if (audit.ShadowDiffDetails.Count < 200)
        {
            audit.ShadowDiffDetails.Add(diff);
        }
    }

    private static void EnsureRecordLength(string recordCode, string rendered, int expectedLength)
    {
        if (rendered.Length != expectedLength)
        {
            throw new InvalidOperationException($"RecordCode={recordCode} renderizó longitud {rendered.Length} y se esperaba {expectedLength}.");
        }
    }

    private static string ResolveSettlementPolicyByChamber(string? chamberCode)
    {
        return string.Equals(chamberCode, "CENIT", StringComparison.OrdinalIgnoreCase)
            ? "CENIT_BLANK_ONLY"
            : "ACH_BLANK_OR_JULIAN3";
    }

    private async Task PersistGenerationAuditAsync(NachaGenerationAuditResult audit, int? profileId, CancellationToken ct)
    {
        if (!profileId.HasValue)
        {
            return;
        }

        try
        {
            var type7Compared = Math.Max(1, audit.Type7GeneratedTableDriven);
            var type7DiffCount = audit.Type7DiffByField.Values.Sum();
            var type7MatchRate = 100m * (type7Compared - Math.Min(type7Compared, type7DiffCount)) / type7Compared;
            audit.Trace.Add($"Type7Summary:Candidates={audit.Type7TotalCandidates};New={audit.Type7GeneratedTableDriven};Legacy={audit.Type7GeneratedLegacy};Diffs={type7DiffCount};MatchRate={type7MatchRate:0.00}%");
            audit.Trace.Add($"ShadowCompareSummary:DiffCount={audit.ShadowDiffCount};StoredDetails={audit.ShadowDiffDetails.Count}");

            _context.HistConfigChanges.Add(new Domain.Models.ACH.Config.HistConfigChange
            {
                ProfileId = profileId.Value,
                EntityName = "NachaFileBuilder",
                EntityId = Guid.NewGuid().ToString("N"),
                ChangeType = "GENERATION_TRACE",
                BeforeJson = null,
                AfterJson = JsonSerializer.Serialize(audit),
                ChangedAtUtc = DateTime.UtcNow,
                ChangedBy = "system-runtime",
                CorrelationId = $"NACHA-GEN-{DateTime.UtcNow:yyyyMMddHHmmss}"
            });
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "No fue posible persistir traza de generación NACHA.");
        }
    }

    private static long SumBatchDebit(AchBatch batch)
    {
        return batch.Transactions
            .Where(tx => tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
            .Sum(tx => (long)(tx.Amount * 100));
    }

    private static long SumBatchCredit(AchBatch batch)
    {
        return batch.Transactions
            .Where(tx => tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
            .Sum(tx => (long)(tx.Amount * 100));
    }

    private async Task<IReadOnlyList<NachaRecordDefinition>> LoadDefinitionsAsync(CancellationToken ct)
    {
        var definitions = await _context.NachaRecordDefinitions
            .AsNoTracking()
            .Where(d => d.IsEnabled)
            .OrderBy(d => d.Sequence)
            .ToListAsync(ct);

        return definitions.Count > 0 ? definitions : BuildDefaultDefinitions();
    }

    private static IReadOnlyList<NachaRecordDefinition> BuildDefaultDefinitions()
    {
        return new List<NachaRecordDefinition>
        {
            new()
            {
                RecordCode = "1",
                Sequence = 10,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "5",
                Sequence = 20,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "6",
                Sequence = 30,
                SourceType = NachaRecordSourceType.Custom,
                SourceName = nameof(AchTransaction),
                FilterKey = "BatchId",
                IsEnabled = true
            },
            new()
            {
                RecordCode = "7",
                Sequence = 40,
                SourceType = NachaRecordSourceType.Custom,
                SourceName = nameof(AchTransactionAddenda),
                FilterKey = "BatchId",
                IsEnabled = true
            },
            new()
            {
                RecordCode = "8",
                Sequence = 50,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "9",
                Sequence = 60,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            }
        };
    }

    private async Task<NachaHeader?> LoadHeaderAsync(string cycleId, CancellationToken ct)
    {
        return await _context.NachaHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.AchCycleId == cycleId, ct);
    }

    private sealed record FileHeaderRecord
    {
        public string? PriorityCode { get; init; }
        public string? ImmediateDestination { get; init; }
        public string? ImmediateOrigin { get; init; }
        public DateTime FileCreationDate { get; init; }
        public DateTime FileCreationTime { get; init; }
        public string? FileIdModifier { get; init; }
        public string? RecordSize { get; init; }
        public string? BlockingFactor { get; init; }
        public string? FormatCode { get; init; }
        public string? ImmediateDestinationName { get; init; }
        public string? ImmediateOriginName { get; init; }
        public string? ReferenceCode { get; init; }
        public string? CycleName { get; init; }
        public DateTime ProcessingDate { get; init; }

        public static FileHeaderRecord From(AchCycle cycle, IReadOnlyCollection<AchTransaction> transactions, NachaHeader? header)
        {
            var now = DateTime.UtcNow;
            var firstTransaction = transactions
                .OrderBy(t => t.Id)
                .FirstOrDefault();

            var destinationDfi = CoalesceNonEmpty(
                header?.ImmediateDestination,
                cycle.ClearingHouse?.OriginCode,
                firstTransaction?.ReceivingDFI);

            var originDfi = CoalesceNonEmpty(
                header?.ImmediateOrigin,
                firstTransaction?.OriginatingDFI);

            var destinationName = CoalesceNonEmpty(
                header?.ImmediateDestinationName,
                cycle.ClearingHouse?.Name,
                firstTransaction?.DestinationInstitution?.Name);

            var originName = CoalesceNonEmpty(
                header?.ImmediateOriginName,
                firstTransaction?.SourceInstitution?.Name);

            return new FileHeaderRecord
            {
                PriorityCode = CoalesceNonEmpty(header?.PriorityCode, "01"),
                ImmediateDestination = destinationDfi,
                ImmediateOrigin = originDfi,
                FileCreationDate = ParseDate(header?.FileCreationDate) ?? now,
                FileCreationTime = ParseTime(header?.FileCreationTime) ?? now,
                FileIdModifier = CoalesceNonEmpty(header?.FileIdModifier, "A"),
                RecordSize = string.IsNullOrWhiteSpace(header?.RecordSize) ? "106" : header!.RecordSize,
                BlockingFactor = string.IsNullOrWhiteSpace(header?.BlockingFactor) ? "10" : header!.BlockingFactor,
                FormatCode = string.IsNullOrWhiteSpace(header?.FormatCode) ? "1" : header!.FormatCode,
                ImmediateDestinationName = destinationName,
                ImmediateOriginName = originName,
                ReferenceCode = CoalesceNonEmpty(header?.ReferenceCode, cycle.CycleName),
                CycleName = cycle.CycleName,
                ProcessingDate = cycle.ProcessingDate
            };
        }

        private static string? CoalesceNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        }

        private static DateTime? ParseDate(string? value)
        {
            return DateTime.TryParse(value, out var parsed) ? parsed : null;
        }

        private static DateTime? ParseTime(string? value)
        {
            return DateTime.TryParse(value, out var parsed) ? parsed : null;
        }
    }

    private sealed record CompanyEntryDescriptionCatalogItem(string Term, string StandardEntryClassCode);
    private sealed record BatchCalculation(
        IReadOnlyList<AchTransaction> Transactions,
        int EntryAddendaCount,
        int AddendaOnlyCount,
        long BatchDebit,
        long BatchCredit,
        string StandardEntryClassCode,
        string BatchEntryDescription);

    private sealed record ReceiverLookup(
        IReadOnlyDictionary<(string Document, string Account), IReadOnlyList<Customer>> CustomersByDocumentAndAccount,
        IReadOnlyDictionary<string, List<Customer>> CustomersByAccount)
    {
        public static readonly ReceiverLookup Empty = new(
            new Dictionary<(string Document, string Account), IReadOnlyList<Customer>>(),
            new Dictionary<string, List<Customer>>(StringComparer.Ordinal));
    }

    private sealed record PrenoteLookupKey(
        int DestinationInstitutionId,
        string DestinationAccountNumber,
        string TransactionCode);

    private static string ResolveBatchEntryDescription(AchBatch batch, IReadOnlyCollection<AchTransaction> batchTransactions)
    {
        var description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
        var creditLikeCount = batchTransactions.Count(tx => tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification);
        return creditLikeCount > 1 ? "MULTICREDIT" : description;
    }


    private sealed record BatchHeaderRecord
    {
        public string ServiceClassCode { get; init; } = string.Empty;
        public string CompanyName { get; init; } = string.Empty;
        public string CompanyDiscretionaryData { get; init; } = string.Empty;
        public string CompanyIdentification { get; init; } = string.Empty;
        public string StandardEntryClassCode { get; init; } = "PPD";
        public string CompanyEntryDescription { get; init; } = string.Empty;
        public DateTime CompanyDescriptiveDate { get; init; }
        public DateTime EffectiveEntryDate { get; init; }
        public string SettlementDate { get; init; } = string.Empty;
        public string OriginatorStatusCode { get; init; } = "1";
        public string OriginatingDFI { get; init; } = string.Empty;
        public int BatchNumber { get; init; }

        public static BatchHeaderRecord From(AchBatch batch, string standardEntryClassCode, int batchNumber, string companyEntryDescription)
        {
            if (standardEntryClassCode is not ("PPD" or "CCD"))
            {
                throw new InvalidOperationException("Error Fatal ID 20: Tipo de servicio del lote inválido. Solo se permite PPD o CCD.");
            }

            return new BatchHeaderRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                CompanyName = batch.CompanyName,
                CompanyDiscretionaryData = string.Empty,
                CompanyIdentification = batch.CompanyIdentification,
                StandardEntryClassCode = standardEntryClassCode,
                CompanyEntryDescription = companyEntryDescription,
                CompanyDescriptiveDate = batch.EffectiveEntryDate,
                EffectiveEntryDate = batch.EffectiveEntryDate,
                SettlementDate = string.Empty,
                OriginatorStatusCode = "1",
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }
    }

    private static string ResolveStandardEntryClassCode(
        AchBatch batch,
        IReadOnlyCollection<AchTransaction> batchTransactions,
        IReadOnlyCollection<CompanyEntryDescriptionCatalogItem> catalog)
    {
        string description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
        var termMatch = catalog.FirstOrDefault(item => string.Equals(item.Term, description, StringComparison.OrdinalIgnoreCase));
        if (termMatch is not null)
        {
            return termMatch.StandardEntryClassCode;
        }

        throw new InvalidOperationException("Error Fatal ID 20: Tipo de servicio del lote inválido. El concepto no está parametrizado en el catálogo.");
    }

    private sealed record EntryDetailRecord
    {
        public string TransactionCode { get; init; } = string.Empty;
        public string ReceivingDFI { get; init; } = string.Empty;
        public string CheckDigit { get; init; } = string.Empty;
        public string DestinationAccountNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string RecipientIdNumber { get; init; } = string.Empty;
        public string ReceiverName { get; init; } = string.Empty;
        public string DiscretionaryData { get; init; } = string.Empty;
        public string AddendumIndicator { get; init; } = "1";
        public string TraceNumber { get; init; } = string.Empty;
        public string CompanyIdentification { get; init; } = string.Empty;

        public static EntryDetailRecord From(AchTransaction tx, string receiverName, int receivingDfiLength)
        {
            var receivingDfi = (tx.ReceivingDFI ?? string.Empty).Trim();
            if (receivingDfi.Length != receivingDfiLength || receivingDfi.Any(c => !char.IsDigit(c)))
            {
                throw new InvalidOperationException($"Error Fatal ID 35: el Código Entidad Participante Receptor (posiciones 4-11) debe contener {receivingDfiLength} dígitos numéricos según configuración NACHA.");
            }

            if (receivingDfiLength != 8)
            {
                throw new InvalidOperationException($"Configuración inválida: la longitud del campo ReceivingDFI en registro tipo 6 es {receivingDfiLength}. El cálculo de dígito de chequeo ACH requiere exactamente 8.");
            }

            var expectedCheckDigit = DigitoChequeoHelper.CalcularDigitoChequeo(receivingDfi);
            var destinationCheckDigit = tx.DestinationInstitution?.CheckDigit?.Trim();
            var checkDigit = string.IsNullOrWhiteSpace(destinationCheckDigit) ? expectedCheckDigit : destinationCheckDigit;

            if (!string.Equals(checkDigit, expectedCheckDigit, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Error Fatal ID 35: inconsistencia de dígito de chequeo para la entidad receptora {receivingDfi}. Base de datos={checkDigit}, calculado={expectedCheckDigit}.");
            }

            return new EntryDetailRecord
            {
                TransactionCode = tx.TransactionCode,
                ReceivingDFI = receivingDfi,
                CheckDigit = checkDigit,
                DestinationAccountNumber = tx.DestinationAccountNumber,
                Amount = tx.Amount,
                RecipientIdNumber = tx.RecipientIdNumber,
                ReceiverName = receiverName,
                DiscretionaryData = tx.DiscretionaryData,
                AddendumIndicator = "1",
                TraceNumber = tx.TraceNumber,
                CompanyIdentification = tx.CompanyIdentification
            };
        }
    }

    private async Task<IReadOnlyList<EntryDetailRecord>> BuildEntryDetailRecordsAsync(
        IReadOnlyList<AchTransaction> transactions,
        CancellationToken ct)
    {
        var records = new List<EntryDetailRecord>(transactions.Count);
        var receivingDfiLength = await ResolveReceivingDfiLengthFromLayoutAsync(ct);
        var receiverLookup = await BuildReceiverLookupAsync(transactions, ct);

        foreach (var transaction in transactions.OrderBy(t => t.Id))
        {
            var receiverName = await ResolveReceiverNameForType6Async(transaction, receiverLookup, ct);
            var normalizedReceiverName = NachaReceiverNameHelper.SanitizeForType6(receiverName);

            if (string.IsNullOrWhiteSpace(normalizedReceiverName))
            {
                throw new InvalidOperationException($"Error Fatal ID 22: la transacción {transaction.Id} no tiene Nombre del Usuario Receptor válido para posiciones 63-84 del registro tipo 6. Revise Cliente/Tercero asociado y número de cuenta destino.");
            }

            records.Add(EntryDetailRecord.From(transaction, normalizedReceiverName, receivingDfiLength));
        }

        return records;
    }

    private async Task<int> ResolveReceivingDfiLengthFromLayoutAsync(CancellationToken ct)
    {
        var fieldLength = await _context.NachaRecordFields
            .AsNoTracking()
            .Where(field =>
                field.Layout.RecordCode == "6" &&
                (field.FieldName == "ReceivingDFI" || field.DbColumn == "ReceivingDFI"))
            .Select(field => (int?)field.Length)
            .FirstOrDefaultAsync(ct);

        return fieldLength ?? 8;
    }

    private async Task<string> ResolveReceiverNameForType6Async(AchTransaction transaction, CancellationToken ct)
    {
        return await ResolveReceiverNameForType6Async(transaction, receiverLookup: null, ct);
    }

    private async Task<string> ResolveReceiverNameForType6Async(
        AchTransaction transaction,
        ReceiverLookup? receiverLookup,
        CancellationToken ct)
    {
        var destinationAccount = (transaction.DestinationAccountNumber ?? string.Empty).Trim();
        var recipientId = (transaction.RecipientIdNumber ?? string.Empty).Trim();

        if (receiverLookup is not null)
        {
            if (!string.IsNullOrWhiteSpace(recipientId) &&
                receiverLookup.CustomersByDocumentAndAccount.TryGetValue((recipientId, destinationAccount), out var exactMatches) &&
                exactMatches.Count > 0)
            {
                return BuildReceiverName(exactMatches[0]);
            }

            if (receiverLookup.CustomersByAccount.TryGetValue(destinationAccount, out var accountMatches) && accountMatches.Count == 1)
            {
                return BuildReceiverName(accountMatches[0]);
            }
        }
        else
        {
            Customer? customer;
            if (!string.IsNullOrWhiteSpace(recipientId))
            {
                customer = await _context.Customers
                    .AsNoTracking()
                    .Include(c => c.Accounts)
                    .FirstOrDefaultAsync(c => c.DocumentNumber == recipientId && c.Accounts.Any(a => a.AccountNumber == destinationAccount), ct);

                if (customer is not null)
                {
                    return BuildReceiverName(customer);
                }
            }

            var accountMatches = await _context.Customers
                .AsNoTracking()
                .Include(c => c.Accounts)
                .Where(c => c.Accounts.Any(a => a.AccountNumber == destinationAccount))
                .ToListAsync(ct);

            if (accountMatches.Count == 1)
            {
                return BuildReceiverName(accountMatches[0]);
            }
        }

        if (!string.IsNullOrWhiteSpace(transaction.RecipientIdNumber))
        {
            return transaction.RecipientIdNumber;
        }

        return $"USUARIO {transaction.Id}";
    }

    private static string BuildReceiverName(Customer customer)
    {
        if (string.Equals(customer.PersonType, "PJ", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(customer.CompanyName))
        {
            return customer.CompanyName;
        }

        var parts = new[] { customer.FirstName, customer.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        var fullName = string.Join(' ', parts).Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return customer.CompanyName ?? string.Empty;
    }

    private async Task ValidateTransactionsForSendAsync(IEnumerable<AchTransaction> transactions, CancellationToken ct)
    {
        var txList = transactions as IReadOnlyList<AchTransaction> ?? transactions.ToList();
        var prenoteLookup = await BuildPrenoteLookupAsync(txList, ct);

        foreach (var tx in txList)
        {
            if (tx.IsPrenotification && tx.Amount != 0)
            {
                throw new InvalidOperationException($"La prenotificación {tx.Id} debe tener valor 0.");
            }

            if (!tx.IsPrenotification)
            {
                var prenoteDate = await GetPrenoteDateAsync(tx, prenoteLookup, ct);
                if (prenoteDate is null)
                {
                    throw new InvalidOperationException($"La transacción {tx.Id} no tiene prenotificación previa.");
                }

                var minDate = AddBusinessDays(prenoteDate.Value.Date, 3);
                if (tx.EffectiveEntryDate.Date < minDate)
                {
                    throw new InvalidOperationException($"La transacción {tx.Id} no cumple los 3 días hábiles desde la prenotificación.");
                }
            }
        }
    }

    private async Task<DateTime?> GetPrenoteDateAsync(AchTransaction tx, CancellationToken ct)
    {
        return await GetPrenoteDateAsync(tx, prenoteLookup: null, ct);
    }

    private async Task<DateTime?> GetPrenoteDateAsync(
        AchTransaction tx,
        IReadOnlyDictionary<PrenoteLookupKey, DateTime>? prenoteLookup,
        CancellationToken ct)
    {
        var prenoteCode = ResolvePrenoteCode(tx.TransactionCode);
        if (string.IsNullOrWhiteSpace(prenoteCode))
        {
            return null;
        }

        if (prenoteLookup is not null)
        {
            var key = new PrenoteLookupKey(
                tx.DestinationInstitutionId,
                (tx.DestinationAccountNumber ?? string.Empty).Trim(),
                prenoteCode);

            return prenoteLookup.TryGetValue(key, out var date) ? date : null;
        }

        return await _context.AchTransactions
            .AsNoTracking()
            .Where(t => t.IsPrenotification
                        && t.DestinationInstitutionId == tx.DestinationInstitutionId
                        && t.DestinationAccountNumber == tx.DestinationAccountNumber
                        && t.TransactionCode == prenoteCode)
            .OrderByDescending(t => t.EffectiveEntryDate)
            .Select(t => (DateTime?)t.EffectiveEntryDate.Date)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<ReceiverLookup> BuildReceiverLookupAsync(
        IReadOnlyList<AchTransaction> transactions,
        CancellationToken ct)
    {
        var destinationAccounts = transactions
            .Select(tx => (tx.DestinationAccountNumber ?? string.Empty).Trim())
            .Where(account => !string.IsNullOrWhiteSpace(account))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (destinationAccounts.Length == 0)
        {
            return ReceiverLookup.Empty;
        }

        var customers = await _context.Customers
            .AsNoTracking()
            .Include(c => c.Accounts)
            .Where(c => c.Accounts.Any(a => destinationAccounts.Contains(a.AccountNumber)))
            .ToListAsync(ct);

        var byDocumentAndAccount = new Dictionary<(string Document, string Account), List<Customer>>();
        var byAccount = new Dictionary<string, List<Customer>>(StringComparer.Ordinal);

        foreach (var customer in customers)
        {
            var document = (customer.DocumentNumber ?? string.Empty).Trim();
            foreach (var account in customer.Accounts)
            {
                var accountNumber = (account.AccountNumber ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    continue;
                }

                if (!byAccount.TryGetValue(accountNumber, out var list))
                {
                    list = new List<Customer>();
                    byAccount[accountNumber] = list;
                }

                if (!list.Any(existing => existing.Id == customer.Id))
                {
                    list.Add(customer);
                }

                if (!string.IsNullOrWhiteSpace(document))
                {
                    var documentAccountKey = (document, accountNumber);
                    if (!byDocumentAndAccount.TryGetValue(documentAccountKey, out var exactList))
                    {
                        exactList = new List<Customer>();
                        byDocumentAndAccount[documentAccountKey] = exactList;
                    }

                    if (!exactList.Any(existing => existing.Id == customer.Id))
                    {
                        exactList.Add(customer);
                    }
                }
            }
        }

        return new ReceiverLookup(
            byDocumentAndAccount.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Customer>)pair.Value),
            byAccount);
    }

    private async Task<IReadOnlyDictionary<PrenoteLookupKey, DateTime>> BuildPrenoteLookupAsync(
        IReadOnlyList<AchTransaction> transactions,
        CancellationToken ct)
    {
        var keys = transactions
            .Where(tx => !tx.IsPrenotification)
            .Select(tx => new
            {
                Tx = tx,
                PrenoteCode = ResolvePrenoteCode(tx.TransactionCode)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.PrenoteCode))
            .Select(item => new PrenoteLookupKey(
                item.Tx.DestinationInstitutionId,
                (item.Tx.DestinationAccountNumber ?? string.Empty).Trim(),
                item.PrenoteCode!))
            .Distinct()
            .ToArray();

        if (keys.Length == 0)
        {
            return new Dictionary<PrenoteLookupKey, DateTime>();
        }

        var institutionIds = keys.Select(k => k.DestinationInstitutionId).Distinct().ToArray();
        var accounts = keys.Select(k => k.DestinationAccountNumber).Distinct(StringComparer.Ordinal).ToArray();
        var codes = keys.Select(k => k.TransactionCode).Distinct(StringComparer.Ordinal).ToArray();
        var keySet = keys.ToHashSet();

        var prenotes = await _context.AchTransactions
            .AsNoTracking()
            .Where(t =>
                t.IsPrenotification &&
                institutionIds.Contains(t.DestinationInstitutionId) &&
                accounts.Contains(t.DestinationAccountNumber) &&
                codes.Contains(t.TransactionCode))
            .Select(t => new
            {
                t.DestinationInstitutionId,
                t.DestinationAccountNumber,
                t.TransactionCode,
                Date = t.EffectiveEntryDate.Date
            })
            .ToListAsync(ct);

        var lookup = prenotes
            .Select(item => new
            {
                Key = new PrenoteLookupKey(
                    item.DestinationInstitutionId,
                    (item.DestinationAccountNumber ?? string.Empty).Trim(),
                    item.TransactionCode),
                item.Date
            })
            .Where(item => keySet.Contains(item.Key))
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.Max(x => x.Date));

        return lookup;
    }

    private static string? ResolvePrenoteCode(string transactionCode)
    {
        return transactionCode switch
        {
            "22" => "23",
            "27" => "28",
            "32" => "33",
            "37" => "38",
            "52" => "53",
            "55" => "57",
            _ => null
        };
    }

    private DateTime AddBusinessDays(DateTime start, int days)
    {
        var date = start;
        var remaining = days;
        var currentYear = date.Year;
        var holidays = _holidayService.GetHolidays(currentYear)
            .Select(h => h.Date)
            .ToHashSet();

        while (remaining > 0)
        {
            date = date.AddDays(1);

            if (date.Year != currentYear)
            {
                currentYear = date.Year;
                holidays = _holidayService.GetHolidays(currentYear)
                    .Select(h => h.Date)
                    .ToHashSet();
            }

            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isHoliday = holidays.Contains(DateOnly.FromDateTime(date));
            if (!isWeekend && !isHoliday)
            {
                remaining--;
            }
        }

        return date;
    }

    private sealed record BatchControlRecord
    {
        public string ServiceClassCode { get; init; } = string.Empty;
        public int EntryAddendaCount { get; init; }
        public long EntryHash { get; init; }
        public long TotalDebitAmount { get; init; }
        public long TotalCreditAmount { get; init; }
        public string CompanyIdentification { get; init; } = string.Empty;
        public string MessageAuthenticationCode { get; init; } = string.Empty;
        public string OriginatingDFI { get; init; } = string.Empty;
        public int BatchNumber { get; init; }

        public static BatchControlRecord From(AchBatch batch, int entryAddendaCount, long batchDebit, long batchCredit, int batchNumber)
        {
            return new BatchControlRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                EntryAddendaCount = entryAddendaCount,
                EntryHash = ComputeEntryHash(batch.Transactions),
                TotalDebitAmount = batchDebit,
                TotalCreditAmount = batchCredit,
                CompanyIdentification = batch.CompanyIdentification,
                MessageAuthenticationCode = string.Empty,
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }
    }

    private sealed record FileControlRecord
    {
        public int BatchCount { get; init; }
        public int BlockCount { get; init; }
        public int EntryAddendaCount { get; init; }
        public long EntryHash { get; init; }
        public long TotalDebitAmount { get; init; }
        public long TotalCreditAmount { get; init; }
        public string CycleName { get; init; } = string.Empty;

        public static FileControlRecord From(AchCycle cycle, IEnumerable<AchBatch> batches, int batchCount, int blockCount, int entryAddendaCount, long totalDebit, long totalCredit)
        {
            return new FileControlRecord
            {
                BatchCount = batchCount,
                BlockCount = blockCount,
                EntryAddendaCount = entryAddendaCount,
                EntryHash = ComputeEntryHash(batches.SelectMany(b => b.Transactions)),
                TotalDebitAmount = totalDebit,
                TotalCreditAmount = totalCredit,
                CycleName = cycle.CycleName
            };
        }
    }

    private static long ComputeEntryHash(IEnumerable<AchTransaction> transactions)
    {
        const long maxHash = 10_000_000_000L;
        long hash = 0;

        foreach (var tx in transactions)
        {
            var dfi = new string((tx.ReceivingDFI ?? string.Empty).Where(char.IsDigit).ToArray());
            if (dfi.Length == 0)
            {
                continue;
            }

            var first8 = dfi.Length >= 8 ? dfi[..8] : dfi;
            if (long.TryParse(first8, out var value))
            {
                hash = (hash + value) % maxHash;
            }
        }

        return hash;
    }
}
