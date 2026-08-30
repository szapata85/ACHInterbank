using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
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
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
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
    private readonly INachaControlTotalsCalculator _controlTotalsCalculator;
    private readonly IOperationalTimeSnapshotProvider _operationalTimeProvider;
    private readonly ICycleCalendarGuard _calendarGuard;

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
        IBatchNumberGenerator? batchNumberGenerator = null,
        INachaControlTotalsCalculator? controlTotalsCalculator = null,
        IOperationalTimeSnapshotProvider? operationalTimeProvider = null,
        ICycleCalendarGuard? calendarGuard = null)
    {
        _context = context;
        _ = holidayService;
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
        _controlTotalsCalculator = controlTotalsCalculator ?? new NachaControlTotalsCalculator();
        _operationalTimeProvider = operationalTimeProvider ?? new OperationalTimeSnapshotProvider();
        _calendarGuard = calendarGuard ?? new CycleCalendarGuard(context);
    }

    public async Task<NachaReturnOutBuildResult> BuildReturnOutAsync(
        NachaReturnOutBuildRequest request,
        CancellationToken ct = default)
    {
        if (request.Batches.Count == 0 || request.Batches.Any(batch => batch.Entries.Count == 0))
        {
            throw new NachaGenerationException("NACHA_RETURN_OUT_EMPTY", "ReturnOut requiere al menos un lote y una entrada.");
        }

        if (_configResolver is null)
        {
            throw new NachaGenerationException("NACHA_PROFILE_NOT_PUBLISHED", "Resolver NACHA Opción C no registrado. No se habilita fallback legacy.");
        }

        var recordCodes = new[] { "1", "5", "6", "7", "8", "9" };
        var clearingHouseCode = request.ClearingHouseCode.Trim().ToUpperInvariant();
        var isCenit = string.Equals(clearingHouseCode, "CENIT", StringComparison.Ordinal);
        var isCenitRor = isCenit && string.Equals(request.FlowTypeCode, CenitReturnOfReturn2026Layout.FlowTypeCode, StringComparison.Ordinal);
        if (isCenitRor && request.Batches.Any(batch => !string.Equals(batch.StandardEntryClassCode?.Trim(), "PPD", StringComparison.OrdinalIgnoreCase)))
        {
            var includesCtx = request.Batches.Any(batch => string.Equals(batch.StandardEntryClassCode?.Trim(), "CTX", StringComparison.OrdinalIgnoreCase));
            throw new NachaGenerationException(
                includesCtx ? CenitReturnOfReturn2026Layout.CtxScopeStatus : CenitReturnOfReturn2026Layout.CcdScopeStatus,
                "El contrato específico de ROR CENIT vigente está definido para PPD.");
        }
        var recordLength = isCenit ? CenitReturnOut2026Layout.RecordLength : AchColReturnOutV35Layout.RecordLength;
        var blockingFactor = isCenit ? CenitReturnOut2026Layout.BlockingFactor : AchColReturnOutV35Layout.BlockingFactor;
        var resolutionRequest = new NachaConfigResolutionRequest
        {
            ClearingHouseCode = clearingHouseCode,
            FlowTypeCode = request.FlowTypeCode,
            DirectionCode = "SALIDA",
            ProcessDateUtc = request.CreatedAtUtc,
            RecordCodes = recordCodes,
            RequireHomologated = false,
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Flow"] = isCenitRor ? "RETURN_OF_RETURN_OUT" : "RETURN_OUT",
                ["NormativeVersion"] = request.NormativeVersion
            }
        };
        var resolution = await _configResolver.ResolveAsync(resolutionRequest, ct);
        if (resolution.SelectionStatus == NachaProfileSelectionStatus.ProfileAmbiguous)
        {
            throw new NachaGenerationException("NACHA_PROFILE_AMBIGUOUS", $"Existe más de un perfil {clearingHouseCode}/{request.FlowTypeCode}/SALIDA aplicable.");
        }

        if (!resolution.Success || resolution.Profile is null)
        {
            throw new NachaGenerationException("NACHA_PROFILE_NOT_PUBLISHED", $"No existe perfil NACHA-M publicado/vigente para {clearingHouseCode}/{request.FlowTypeCode}/SALIDA.");
        }

        if (resolution.UsedFallback)
        {
            throw new NachaGenerationException("NACHA_LEGACY_GENERATION_DISABLED", $"ReturnOut {clearingHouseCode} no permite fallback físico legacy.");
        }

        var normativeVersion = resolution.Profile.Tags
            .FirstOrDefault(tag => string.Equals(tag.TagKey, "NormativeVersion", StringComparison.OrdinalIgnoreCase))?.TagValue;
        if (!string.Equals(normativeVersion, request.NormativeVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new NachaGenerationException("NACHA_RETURN_OUT_PROFILE_VERSION_REQUIRED", $"El perfil ReturnOut seleccionado no declara NormativeVersion={request.NormativeVersion}.");
        }

        var audit = new NachaGenerationAuditResult
        {
            Mode = "TABLE_DRIVEN",
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseName = request.ClearingHouseName,
            ProfileId = resolution.Profile.Id,
            ProfileCode = resolution.Profile.ProfileCode,
            ProfileVersion = $"{resolution.Profile.VersionMajor}.{resolution.Profile.VersionMinor}",
            ProfileStatus = resolution.Profile.Status?.Code,
            EffectiveDate = request.CreatedAtUtc,
            LegacyFallbackUsed = false,
            Phase = isCenitRor ? "CENIT_RETURN_OF_RETURN_OUT_2026" : isCenit ? "CENIT_RETURN_OUT_2026" : "RETURN_OUT_V35",
            CorrelationId = $"RETURN-OUT-{request.CreatedAtUtc:yyyyMMddHHmmss}"
        };
        audit.Trace.AddRange(resolution.Trace);
        audit.Trace.Add($"ReturnOutOptionC:Profile={resolution.Profile.ProfileCode};ClearingHouse={clearingHouseCode};Flow={request.FlowTypeCode};Direction=SALIDA;LegacyFallbackUsed=false");
        audit.NewEngineRecordCodes.AddRange(recordCodes);

        foreach (var recordCode in recordCodes)
        {
            ValidateOfficialLayout(recordCode, RequireOfficialLayout(resolution, recordCode), audit);
        }

        var batchTotals = request.Batches.Select(batch => CalculateReturnOutBatchTotals(batch, clearingHouseCode)).ToList();
        var entryAddendaCount = batchTotals.Sum(total => total.EntryAddendaCount);
        var entryHash = batchTotals.Aggregate(0L, (current, total) => (current + total.EntryHash) % 10_000_000_000L);
        var totalDebit = batchTotals.Sum(total => total.TotalDebit);
        var totalCredit = batchTotals.Sum(total => total.TotalCredit);
        var recordsBeforePadding = 2 + request.Batches.Sum(batch => 2 + (batch.Entries.Count * 2));
        var blockCount = (int)Math.Ceiling(recordsBeforePadding / (decimal)blockingFactor);
        var paddingCount = (blockCount * blockingFactor) - recordsBeforePadding;

        var sb = new StringBuilder(recordsBeforePadding * recordLength);
        var lineNumber = 1;
        var recordCount = 0;
        recordCount += AppendOfficialRecords(sb, "1", [ReturnOutValues(
            ("ImmediateDestination", request.ImmediateDestination),
            ("ImmediateOrigin", request.ImmediateOrigin),
            ("FileCreationDate", request.CreatedAtUtc),
            ("FileCreationTime", request.CreatedAtUtc),
            ("FileIdModifier", request.FileIdModifier),
            ("ImmediateDestinationName", request.ImmediateDestinationName),
            ("ImmediateOriginName", request.ImmediateOriginName),
            ("ReferenceCode", request.ReferenceCode))], RequireOfficialLayout(resolution, "1"), audit, ref lineNumber);

        for (var batchIndex = 0; batchIndex < request.Batches.Count; batchIndex++)
        {
            var batch = request.Batches[batchIndex];
            var totals = batchTotals[batchIndex];
            recordCount += AppendOfficialRecords(sb, "5", [ReturnOutValues(
                ("ServiceClassCode", batch.ServiceClassCode), ("CompanyName", batch.CompanyName),
                ("CompanyDiscretionaryData", batch.CompanyDiscretionaryData), ("CompanyIdentification", batch.CompanyIdentification),
                ("StandardEntryClassCode", batch.StandardEntryClassCode), ("CompanyEntryDescription", batch.CompanyEntryDescription),
                ("CompanyDescriptiveDate", batch.CompanyDescriptiveDate), ("EffectiveEntryDate", batch.EffectiveEntryDate),
                ("SettlementDate", batch.SettlementDate), ("OriginatingDfi", batch.OriginatingDfi), ("BatchNumber", batch.BatchNumber))],
                RequireOfficialLayout(resolution, "5"), audit, ref lineNumber);

            foreach (var entry in batch.Entries)
            {
                recordCount += AppendOfficialRecords(sb, "6", [ReturnOutValues(
                    ("TransactionCode", entry.TransactionCode), ("ReceivingDfi", entry.ReceivingDfi), ("CheckDigit", entry.CheckDigit),
                    ("DfiAccountNumber", entry.AccountNumber), ("Amount", entry.Amount),
                    ("IndividualIdentification", entry.IndividualIdentification), ("IndividualName", entry.IndividualName),
                    ("DiscretionaryData", entry.DiscretionaryData), ("TraceNumber", entry.NewTraceNumber))],
                    RequireOfficialLayout(resolution, "6"), audit, ref lineNumber);
                recordCount += AppendOfficialRecords(sb, "7", [ReturnOutValues(
                    ("ReturnReasonCode", entry.ReturnReasonCode), ("OriginalTraceNumber", entry.OriginalTraceNumber),
                    ("DateOfDeath", entry.DeathDate), ("OriginalReceivingDfi", entry.OriginalReceivingDfi),
                    ("AdditionalInformation", entry.AdditionalInformation), ("AddendaSequenceNumber", entry.AddendaSequenceNumber),
                    ("SourceReturnTraceNumber", entry.SourceReturnTraceNumber),
                    ("SourceReturnSettlementDate", entry.SourceReturnSettlementDate),
                    ("SourceReturnReasonCode", entry.SourceReturnReasonCode))],
                    RequireOfficialLayout(resolution, "7"), audit, ref lineNumber);
            }

            recordCount += AppendOfficialRecords(sb, "8", [ReturnOutValues(
                ("ServiceClassCode", batch.ServiceClassCode), ("EntryAddendaCount", totals.EntryAddendaCount),
                ("EntryHash", totals.EntryHash), ("TotalDebitAmount", totals.TotalDebit), ("TotalCreditAmount", totals.TotalCredit),
                ("CompanyIdentification", batch.CompanyIdentification), ("OriginatingDfi", batch.OriginatingDfi), ("BatchNumber", batch.BatchNumber))],
                RequireOfficialLayout(resolution, "8"), audit, ref lineNumber);
        }

        recordCount += AppendOfficialRecords(sb, "9", [ReturnOutValues(
            ("BatchCount", request.Batches.Count), ("BlockCount", blockCount), ("EntryAddendaCount", entryAddendaCount),
            ("EntryHash", entryHash), ("TotalDebitAmount", totalDebit), ("TotalCreditAmount", totalCredit))],
            RequireOfficialLayout(resolution, "9"), audit, ref lineNumber);

        var paddingRecord = BuildPaddingRecord(RequireOfficialLayout(resolution, "9"));
        for (var i = 0; i < paddingCount; i++)
        {
            sb.Append(paddingRecord);
            recordCount++;
        }

        var content = sb.ToString();
        if (recordCount != blockCount * blockingFactor
            || content.Length != recordCount * recordLength)
        {
            throw new NachaGenerationException("NACHA_RECORD_COUNT_MISMATCH", "ReturnOut no cumple bloques de 10 registros de 106 caracteres.");
        }

        audit.TotalRecords = recordCount;
        audit.TotalFields = audit.FieldTraceEntries.Count;
        audit.FileHash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(content)));
        audit.FileIdModifier = new NachaFileIdModifierAudit { DailySequence = 0, ResolvedValue = request.FileIdModifier };
        audit.FileTotals = new NachaFileControlTotalsAudit
        {
            BatchCount = request.Batches.Count,
            BlockCount = blockCount,
            EntryAddendaCount = entryAddendaCount,
            EntryHash = entryHash,
            TotalDebitAmountInCents = DecimalToCents(totalDebit),
            TotalCreditAmountInCents = DecimalToCents(totalCredit),
            PhysicalRecordCountBeforePadding = recordsBeforePadding,
            PaddingRecordCount = paddingCount,
            PhysicalRecordCountAfterPadding = recordCount
        };
        if (request.PersistAudit)
        {
            await PersistGenerationAuditAsync(audit, resolution.Profile.Id, ct, request.CreatedAtUtc);
        }

        return new NachaReturnOutBuildResult(
            content,
            recordCount,
            resolution.Profile.ProfileCode,
            normativeVersion!,
            false,
            blockCount,
            entryAddendaCount,
            entryHash.ToString("D10", System.Globalization.CultureInfo.InvariantCulture));
    }

    private static IReadOnlyDictionary<string, object?> ReturnOutValues(params (string Key, object? Value)[] values)
        => values.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);

    private static ReturnOutBatchTotals CalculateReturnOutBatchTotals(NachaReturnOutBatch batch, string clearingHouseCode)
    {
        var hash = batch.Entries.Aggregate(0L, (current, entry) =>
            (current + long.Parse(entry.ReceivingDfi, CultureInfo.InvariantCulture)) % 10_000_000_000L);
        var debit = batch.Entries.Where(entry => entry.TransactionCode is "26" or "36" or "56").Sum(entry => entry.Amount);
        var credit = batch.Entries.Where(entry => entry.TransactionCode is "21" or "31" or "51").Sum(entry => entry.Amount);
        if (batch.Entries.Any(entry => entry.TransactionCode is not ("21" or "31" or "51" or "26" or "36" or "56")))
        {
            throw new NachaGenerationException("NACHA_ALLOWED_VALUE_INVALID", $"Código de transacción ReturnOut fuera del perfil {clearingHouseCode}.");
        }

        return new ReturnOutBatchTotals(batch.Entries.Count * 2, hash, debit, credit);
    }

    private static long DecimalToCents(decimal amount) => checked((long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    private sealed record ReturnOutBatchTotals(int EntryAddendaCount, long EntryHash, decimal TotalDebit, decimal TotalCredit);

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

        var transactions = batches.SelectMany(b => b.Transactions).ToList();
        var context = new NachaBuildContext
        {
            Cycle = cycle,
            Batches = batches,
            Transactions = transactions
        };
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(context, ct);
        var calendarDecision = await _calendarGuard.EnsureExecutableAsync(cycle, ct);
        if (!calendarDecision.CanExecute)
        {
            throw new CycleDeferredByCalendarException(cycle.Id, calendarDecision);
        }
        EnforceCenitLiveGenerationGate(clearingHouseCode);
        EnforceLiveOfficialMode(clearingHouseCode);
        await _transactionValidationService.ValidateTransactionsForSendAsync(transactions, ct);
        if (IsOfficialTableDrivenMode())
        {
            return await BuildOfficialTableDrivenFileAsync(context, nachaHeader, ct);
        }

        var definitions = await _dataLoader.LoadDefinitionsAsync(ct);
        var layoutCache = await _dataLoader.LoadLayoutsAsync(ct);
        return await BuildFileAsync(context, definitions, layoutCache, nachaHeader, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO ALTERNATIVO: Generar NACHA-M por ciclo
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildNachaFileByCycleAsync(string cycleId, CancellationToken ct = default)
        => (await BuildNachaFileArtifactByCycleAsync(cycleId, ct)).Content;

    public async Task<NachaFileBuildArtifact> BuildNachaFileArtifactByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var result = await BuildNachaFilesByCycleAsync(cycleId, ct);
        if (result.Files.Count == 0)
        {
            throw new InvalidOperationException($"El ciclo {cycleId} no tiene transacciones para exportar.");
        }

        if (result.Files.Count != 1)
        {
            throw new NachaGenerationException(
                "NACHA_MULTI_FILE_RESULT_REQUIRED",
                $"El ciclo {cycleId} produce {result.Files.Count} archivos NACHA-M y debe consumirse mediante el contrato multiarchivo.");
        }

        return result.Files[0];
    }

    public async Task<NachaFileBuildResult> BuildNachaFilesByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var context = await _dataLoader.LoadByCycleAsync(cycleId, ct);
        var cycle = context.Cycle;
        var transactions = context.Transactions;
        var batches = context.Batches;

        if (transactions.Count == 0)
            return NachaFileBuildResult.Empty;

        if (batches.Count == 0)
            throw new InvalidOperationException($"El ciclo {cycleId} no tiene lotes asociados para exportar.");

        var nachaHeader = await _dataLoader.LoadHeaderAsync(cycle.Id, ct);
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(context, ct);
        var calendarDecision = await _calendarGuard.EnsureExecutableAsync(cycle, ct);
        if (!calendarDecision.CanExecute)
        {
            throw new CycleDeferredByCalendarException(cycle.Id, calendarDecision);
        }
        EnforceCenitLiveGenerationGate(clearingHouseCode);
        EnforceLiveOfficialMode(clearingHouseCode);
        await _transactionValidationService.ValidateTransactionsForSendAsync(transactions, ct);
        if (!string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            var content = await BuildContextContentAsync(context, nachaHeader, ct);
            return new NachaFileBuildResult(
            [
                new NachaFileBuildArtifact(
                    content,
                    transactions.Select(transaction => transaction.Id).OrderBy(id => id).ToArray())
            ]);
        }

        var catalog = (await _dataLoader.LoadCompanyEntryDescriptionCatalogAsync(ct))
            .Select(item => new CompanyEntryDescriptionCatalogItem(item.Term, item.StandardEntryClassCode))
            .ToList();
        var transactionsByBatchId = transactions
            .GroupBy(transaction => transaction.AchBatchId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AchTransaction>)group.OrderBy(transaction => transaction.Id).ToArray());
        var sources = batches
            .Where(batch => transactionsByBatchId.ContainsKey(batch.Id))
            .Select(batch => new CenitOutboundSourceBatch(
                batch,
                ResolveStandardEntryClassCode(batch, transactionsByBatchId[batch.Id], catalog),
                transactionsByBatchId[batch.Id]))
            .ToArray();
        var partitions = CenitOutboundFilePartitioner.Partition(sources);
        EnsureCompleteCenitMembership(transactions, partitions);

        if (partitions.Any(partition => partition.ProfileIdentity == CenitOutboundFilePartitioner.CtxProfileIdentity))
        {
            throw new NachaGenerationException(
                "CENIT_CTX_OUTBOUND_PROFILE_NOT_PUBLISHED",
                "La partición CTX fue aislada, pero no existe un perfil publicado con layouts oficiales T5/T6/T7 CTX y multi-adenda.");
        }

        var artifacts = new List<NachaFileBuildArtifact>(partitions.Count);
        foreach (var partition in partitions.OrderBy(item => item.FileIndex))
        {
            var fileContext = BuildPartitionContext(context, partition);
            var content = await BuildContextContentAsync(fileContext, nachaHeader, ct);
            var memberships = partition.Batches
                .Select((batch, index) => new NachaFileBatchMembership(
                    index + 1,
                    batch.SourceBatch.Id,
                    batch.ServiceCode,
                    batch.Transactions.Select(transaction => transaction.Id).OrderBy(id => id).ToArray()))
                .ToArray();
            var profileIdentity = NachaProfileDimensionResolver.ResolveFlowCode(fileContext.Transactions) == "PRENOTIFICACION"
                ? CenitOrdinaryOutbound2026Layout.PrenotificationProfileCode
                : CenitOrdinaryOutbound2026Layout.OriginalProfileCode;
            artifacts.Add(new NachaFileBuildArtifact(
                content,
                fileContext.Transactions.Select(transaction => transaction.Id).OrderBy(id => id).ToArray())
            {
                ProfileIdentity = profileIdentity,
                ServiceCodes = partition.ServiceCodes,
                Batches = memberships
            });
        }

        return new NachaFileBuildResult(artifacts);
    }

    private static void EnsureCompleteCenitMembership(
        IReadOnlyCollection<AchTransaction> sourceTransactions,
        IReadOnlyCollection<CenitOutboundFilePartition> partitions)
    {
        var sourceIds = sourceTransactions.Select(transaction => transaction.Id).OrderBy(id => id).ToArray();
        var emittedIds = partitions
            .SelectMany(partition => partition.Batches)
            .SelectMany(batch => batch.Transactions)
            .Select(transaction => transaction.Id)
            .OrderBy(id => id)
            .ToArray();
        if (!sourceIds.SequenceEqual(emittedIds))
        {
            throw new NachaGenerationException(
                "CENIT_TRANSACTION_MEMBERSHIP_INCOMPLETE",
                "La partición CENIT no preservó exactamente todas las transacciones exportables del ciclo.");
        }
    }

    private async Task<string> BuildContextContentAsync(
        NachaBuildContext context,
        NachaHeader? nachaHeader,
        CancellationToken ct)
    {
        if (IsOfficialTableDrivenMode())
        {
            return await BuildOfficialTableDrivenFileAsync(context, nachaHeader, ct);
        }

        var layoutCache = await _dataLoader.LoadLayoutsAsync(ct);
        var definitions = await _dataLoader.LoadDefinitionsAsync(ct);
        return await BuildFileAsync(context, definitions, layoutCache, nachaHeader, ct);
    }

    private static NachaBuildContext BuildPartitionContext(
        NachaBuildContext source,
        CenitOutboundFilePartition partition)
    {
        var batches = partition.Batches
            .Select((item, index) => CloneBatchForFile(item, index + 1))
            .ToArray();
        return new NachaBuildContext
        {
            Cycle = source.Cycle,
            Batches = batches,
            Transactions = batches.SelectMany(batch => batch.Transactions).ToArray()
        };
    }

    private static AchBatch CloneBatchForFile(CenitOutboundBatchPartition partition, int ordinal)
        => new()
        {
            Id = -ordinal,
            AchCycleId = partition.SourceBatch.AchCycleId,
            AchCycle = partition.SourceBatch.AchCycle,
            ServiceClassCode = partition.SourceBatch.ServiceClassCode,
            CompanyName = partition.SourceBatch.CompanyName,
            CompanyIdentification = partition.SourceBatch.CompanyIdentification,
            CompanyEntryDescription = partition.SourceBatch.CompanyEntryDescription,
            CompanyEntryDescriptionId = partition.SourceBatch.CompanyEntryDescriptionId,
            OriginOrOdfi = partition.SourceBatch.OriginOrOdfi,
            EffectiveEntryDate = partition.SourceBatch.EffectiveEntryDate,
            BatchSequenceNumber = ordinal,
            TotalDebitAmount = partition.Transactions
                .Where(transaction => transaction.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
                .Sum(transaction => transaction.Amount),
            TotalCreditAmount = partition.Transactions
                .Where(transaction => transaction.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
                .Sum(transaction => transaction.Amount),
            Transactions = partition.Transactions.ToList()
        };

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
        var orderedBatches = context.Batches.ToList();
        var clearingHouseCode = context.Cycle.ClearingHouse?.Name?.Contains("CENIT", StringComparison.OrdinalIgnoreCase) == true ? "CENIT" : "ACH";
        var batchNumberAssignment = await ResolveBatchNumberAssignmentAsync(orderedBatches, clearingHouseCode, context.Cycle.ProcessingDate, ct);
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

        var transactionsByBatchId = orderedBatches.ToDictionary(
            batch => batch.Id,
            batch => batch.Transactions.OrderBy(transaction => transaction.Id).ToList());

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
                    audit.Trace.Add($"FileIntegrity:BatchCount={batchCount};EntryAddendaCount={entryAddendaCount};MonetaryTotals=REDACTED;BlockCount={blockCount};PaddingNeeded={paddingNeeded}");

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

    private async Task<string> BuildOfficialTableDrivenFileAsync(
        NachaBuildContext context,
        NachaHeader? header,
        CancellationToken ct)
    {
        // The loader preserves the caller/business order. ACHCOL batch numbers are file-local
        // ordinals and must never be derived, directly or indirectly, from a technical PK.
        var orderedBatches = context.Batches.ToList();
        if (!orderedBatches.Any())
        {
            throw new InvalidOperationException("No se encontraron lotes para exportar.");
        }

        var operationalSnapshot = _operationalTimeProvider.GetOrCreate(
            $"NACHA:{context.Cycle.Id}",
            DateOnly.FromDateTime(context.Cycle.ProcessingDate),
            TimeOnly.FromTimeSpan(context.Cycle.CutoffTime));

        var officialRecordCodes = new[] { "1", "5", "6", "7", "8", "9" };
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(context, ct);
        var resolution = await ResolveOfficialRuntimeConfigAsync(context, clearingHouseCode, officialRecordCodes, ct);
        var lineLength = RequireOfficialLayout(resolution, "9").TotalLength;
        var batchNumberAssignment = await ResolveBatchNumberAssignmentAsync(
            orderedBatches,
            clearingHouseCode,
            operationalSnapshot.BogotaTimestamp,
            ct);
        var batchSequenceById = batchNumberAssignment.BatchNumberByBatchId;

        var audit = new NachaGenerationAuditResult
        {
            Mode = "TABLE_DRIVEN",
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseName = context.Cycle.ClearingHouse?.Name,
            ProfileId = resolution.Profile?.Id,
            ProfileCode = resolution.Profile?.ProfileCode,
            ProfileVersion = resolution.Profile is null ? null : $"{resolution.Profile.VersionMajor}.{resolution.Profile.VersionMinor}",
            ProfileStatus = resolution.Profile?.Status?.Code,
            EffectiveDate = operationalSnapshot.BogotaTimestamp,
            LegacyFallbackUsed = false,
            Phase = "6B.3B",
            CorrelationId = $"NACHA-GEN-{operationalSnapshot.CapturedAtUtc:yyyyMMddHHmmss}"
        };
        audit.Trace.AddRange(resolution.Trace);
        audit.Trace.AddRange(batchNumberAssignment.ScopeTrace.Select(scopeTrace =>
            $"BatchNumberScope:{scopeTrace.Scope};Policy={scopeTrace.PolicyCode};Previous={scopeTrace.PreviousValue};Assigned={scopeTrace.AssignedValue};WasCreated={scopeTrace.WasCreated};Reserved={scopeTrace.ReservedCount}"));
        audit.Trace.Add($"OfficialTableDriven:Profile={resolution.Profile?.ProfileCode};LegacyFallbackUsed=false;Records={string.Join(",", officialRecordCodes)}");
        audit.NewEngineRecordCodes.AddRange(officialRecordCodes);

        var sb = new StringBuilder(capacity: Math.Max(10, context.Transactions.Count * 3 + orderedBatches.Count * 4) * lineLength);
        var transactionsByBatchId = orderedBatches.ToDictionary(
            batch => batch.Id,
            batch => (IReadOnlyList<AchTransaction>)batch.Transactions.OrderBy(transaction => transaction.Id).ToList());

        var recordCount = 0;
        var batchCount = orderedBatches.Count;
        var isAchColOfficial = string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase);
        var dailySequence = header?.CycleNumber > 0 ? header.CycleNumber : 1;
        var fileIdModifier = _controlTotalsCalculator.ResolveFileIdModifier(dailySequence);
        audit.FileIdModifier = new NachaFileIdModifierAudit { DailySequence = dailySequence, ResolvedValue = fileIdModifier };

        var companyEntryDescriptionCatalog = (await _dataLoader.LoadCompanyEntryDescriptionCatalogAsync(ct))
            .Select(item => new CompanyEntryDescriptionCatalogItem(item.Term, item.StandardEntryClassCode))
            .ToList();

        var batchCalculations = new Dictionary<int, BatchCalculation>(orderedBatches.Count);
        foreach (var batch in orderedBatches)
        {
            var batchTransactions = transactionsByBatchId.TryGetValue(batch.Id, out var txs)
                ? txs
                : Array.Empty<AchTransaction>();

            var creditLikeCount = 0;

            foreach (var tx in batchTransactions)
            {
                if (tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
                {
                    creditLikeCount++;
                }
            }

            var description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
            var secCode = ResolveStandardEntryClassCode(batch, batchTransactions, companyEntryDescriptionCatalog);
            var batchDescription = creditLikeCount > 1
                ? ResolveMassCreditDescription(RequireOfficialLayout(resolution, "5"))
                : description;

            batchCalculations[batch.Id] = new BatchCalculation(
                Transactions: batchTransactions,
                StandardEntryClassCode: secCode,
                BatchEntryDescription: batchDescription);
        }

        var type7CandidatesByBatchId = orderedBatches.ToDictionary(
            batch => batch.Id,
            batch => (_type7GenerationStrategy?.BuildCandidates([batch]) ?? BuildFallbackType7Candidates([batch])).ToList());
        var physicalRecordsBeforePadding = 1 + orderedBatches.Count * 2 + context.Transactions.Count + type7CandidatesByBatchId.Values.Sum(x => x.Count) + 1;
        var controlTotals = _controlTotalsCalculator.Calculate(new NachaControlTotalsRequest
        {
            Batches = orderedBatches,
            TransactionsByBatchId = transactionsByBatchId,
            AddendaRecordCountByBatchId = type7CandidatesByBatchId.ToDictionary(x => x.Key, x => x.Value.Count),
            EntryHashSourceFieldPath = ResolveEntryHashSourceFieldPath(RequireOfficialLayout(resolution, "6")),
            BatchEntryHashLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "8"), "ENTRYHASH"),
            FileEntryHashLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "ENTRYHASH"),
            BatchEntryAddendaCountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "8"), "ENTRYADDENDACOUNT"),
            FileEntryAddendaCountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "ENTRYADDENDACOUNT"),
            BatchTotalDebitAmountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "8"), "TOTALDEBITAMOUNT"),
            FileTotalDebitAmountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "TOTALDEBITAMOUNT"),
            BatchTotalCreditAmountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "8"), "TOTALCREDITAMOUNT"),
            FileTotalCreditAmountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "TOTALCREDITAMOUNT"),
            BatchCountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "BATCHCOUNT"),
            BlockCountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "BLOCKCOUNT"),
            PhysicalRecordCountBeforePadding = physicalRecordsBeforePadding,
            BlockSize = ResolveOfficialBlockSize(RequireOfficialLayout(resolution, "1"))
        });
        var controlTotalsByBatchId = controlTotals.BatchTotals.ToDictionary(x => x.BatchId);
        AddControlTotalsAudit(audit, controlTotals);

        var lineNumber = 1;
        try
        {
            foreach (var recordCode in officialRecordCodes)
            {
                ValidateOfficialLayout(recordCode, RequireOfficialLayout(resolution, recordCode), audit);
            }

            if (string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase)
                && resolution.LayoutVariantsByRecordCode.TryGetValue("7", out var type7Layouts))
            {
                foreach (var type7Layout in type7Layouts)
                {
                    ValidateOfficialLayout("7", type7Layout, audit);
                }
            }

            recordCount += AppendOfficialRecords(
                sb,
                "1",
                [FileHeaderRecord.From(context.Cycle, context.Transactions, header, operationalSnapshot, fileIdModifier)],
                RequireOfficialLayout(resolution, "1"),
                audit,
                ref lineNumber);

            foreach (var batch in orderedBatches)
            {
                var calculation = batchCalculations[batch.Id];
                recordCount += AppendOfficialRecords(
                    sb,
                    "5",
                    [BatchHeaderRecord.From(batch, calculation.StandardEntryClassCode, batchSequenceById[batch.Id], calculation.BatchEntryDescription, operationalSnapshot)],
                    RequireOfficialLayout(resolution, "5"),
                    audit,
                    ref lineNumber);

                var type7Candidates = type7CandidatesByBatchId[batch.Id];
                var type7ByTransaction = type7Candidates
                    .GroupBy(candidate => candidate.Transaction.Id)
                    .ToDictionary(group => group.Key, group => group.ToList());
                var entryDetails = await BuildEntryDetailRecordsOfficialAsync(
                    calculation.Transactions,
                    RequireOfficialLayout(resolution, "6"),
                    type7ByTransaction,
                    ct);

                foreach (var entryDetail in entryDetails)
                {
                    recordCount += AppendOfficialRecords(
                        sb,
                        "6",
                        [entryDetail],
                        RequireOfficialLayout(resolution, "6"),
                        audit,
                        ref lineNumber);

                    if (!type7ByTransaction.TryGetValue(entryDetail.TransactionId, out var associatedAddendas))
                    {
                        if (entryDetail.AddendumIndicator == "1")
                        {
                            throw BuildCrossFieldException(
                                "ACHCOL-T6-ADDENDA-INDICATOR",
                                "6/7",
                                "ADDENDARECORDINDICATOR",
                                87,
                                1,
                                "El indicador declara adenda, pero no existe T7 asociado.");
                        }

                        continue;
                    }

                    foreach (var candidate in associatedAddendas.OrderBy(item => item.Addenda.SequenceNumber ?? 1))
                    {
                        if (isAchColOfficial)
                        {
                            ValidateType7Association(entryDetail, candidate);
                        }

                        var type7Layout = isAchColOfficial
                            ? ResolveType7Layout(resolution, candidate)
                            : RequireOfficialLayout(resolution, "7");
                        recordCount += AppendOfficialRecords(
                            sb,
                            "7",
                            [candidate.FieldValues],
                            type7Layout,
                            audit,
                            ref lineNumber);
                    }
                }

                var renderedTransactionIds = entryDetails.Select(entry => entry.TransactionId).ToHashSet();
                if (isAchColOfficial && type7Candidates.Any(candidate => !renderedTransactionIds.Contains(candidate.Transaction.Id)))
                {
                    throw BuildCrossFieldException(
                        "ACHCOL-T7-TRACE-SUFFIX-MATCH",
                        "7",
                        "TRACESUFFIX",
                        88,
                        7,
                        "Existe un T7 sin T6 perteneciente al lote renderizado.");
                }

                var batchTotals = controlTotalsByBatchId[batch.Id];
                var batchControlLine = lineNumber;
                recordCount += AppendOfficialRecords(
                    sb,
                    "8",
                    [BatchControlRecord.From(batch, batchTotals, batchSequenceById[batch.Id])],
                    RequireOfficialLayout(resolution, "8"),
                    audit,
                    ref lineNumber);
                ValidateRenderedControlTotals(audit, "8", batchControlLine, RequireOfficialLayout(resolution, "8"), batchTotals);
            }

            var fileTotals = controlTotals.FileTotals;
            if (recordCount + 1 != fileTotals.PhysicalRecordCountBeforePadding)
            {
                throw new NachaGenerationException("NACHA_RECORD_COUNT_MISMATCH", $"El conteo físico calculado {fileTotals.PhysicalRecordCountBeforePadding} no coincide con el render previo al control {recordCount + 1}.");
            }

            var fileControl = FileControlRecord.From(context.Cycle, fileTotals);
            audit.Trace.Add($"FileIntegrity:BatchCount={batchCount};EntryAddendaCount={fileTotals.EntryAddendaCount};MonetaryTotals=REDACTED;BlockCount={fileTotals.BlockCount};PaddingNeeded={fileTotals.PaddingRecordCount}");
            var fileControlLine = lineNumber;
            recordCount += AppendOfficialRecords(sb, "9", [fileControl], RequireOfficialLayout(resolution, "9"), audit, ref lineNumber);
            ValidateRenderedControlTotals(audit, "9", fileControlLine, RequireOfficialLayout(resolution, "9"), fileTotals);

            if (fileTotals.PaddingRecordCount > 0)
            {
                var paddingRecord = BuildPaddingRecord(RequireOfficialLayout(resolution, "9"));
                for (var i = 0; i < fileTotals.PaddingRecordCount; i++)
                {
                    sb.Append(paddingRecord);
                    audit.FieldTraceEntries.Add(new NachaGenerationTraceEntry
                    {
                        TraceId = audit.TraceId,
                        RecordType = "9",
                        RecordSequence = i + 1,
                        LineNumber = lineNumber++,
                        FieldName = "PADDING_RECORD",
                        DisplayName = "Padding record",
                        PositionStart = 1,
                        PositionEnd = lineLength,
                        Length = lineLength,
                        SourceType = "CALCULATED",
                        CalculationType = "PaddingRecord",
                        RawValueSanitized = "9",
                        RenderedValue = "[PADDING;Length=106]",
                        RuntimeRenderedValue = paddingRecord,
                        RenderedLength = lineLength,
                        ValidationStatus = "Ok",
                        GeneratedLinePreviewSanitized = $"RecordType=9;Length={lineLength};Category=PADDING",
                        ValueStartIndex = 1,
                        ValueEndIndex = lineLength
                    });
                }
            }
        }
        catch (NachaGenerationException ex)
        {
            audit.Status = "Failed";
            audit.ErrorCode = ex.Code;
            audit.TotalRecords = lineNumber - 1;
            audit.TotalFields = audit.FieldTraceEntries.Count;
            await PersistGenerationAuditAsync(audit, resolution.Profile?.Id, ct, operationalSnapshot.CapturedAtUtc);
            throw;
        }

        var fileContent = sb.ToString();
        audit.TotalRecords = fileContent.Length / lineLength;
        audit.TotalFields = audit.FieldTraceEntries.Count;
        audit.FileHash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(fileContent)));
        await PersistGenerationAuditAsync(audit, resolution.Profile?.Id, ct, operationalSnapshot.CapturedAtUtc);
        _nachaSemanticValidator.Validate(fileContent, context);
        return fileContent;
    }

    private int AppendOfficialRecords(
        StringBuilder sb,
        string recordCode,
        IEnumerable<object> records,
        CfgLayoutVariant layout,
        NachaGenerationAuditResult audit,
        ref int lineNumber)
    {
        var count = 0;
        foreach (var record in records)
        {
            sb.Append(RenderOfficialRecord(recordCode, record, layout, audit, count + 1, lineNumber));
            lineNumber++;
            count++;
        }

        return count;
    }

    private Task<BatchNumberAssignmentResult> ResolveBatchNumberAssignmentAsync(
        IReadOnlyList<AchBatch> orderedBatches,
        string clearingHouseCode,
        DateTime processingDate,
        CancellationToken ct)
    {
        if (string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase))
        {
            if (orderedBatches.Count > 9_999_999)
            {
                throw new NachaGenerationException(
                    "NACHA_BATCH_NUMBER_OVERFLOW",
                    "La cantidad de lotes excede la capacidad normativa de siete posiciones.",
                    ruleId: "ACHCOL-T5-BATCH-NUMBER",
                    chamber: "ACHCOL",
                    recordType: "5/8",
                    fieldName: "BATCHNUMBER",
                    cause: "Ordinal de lote fuera de rango.",
                    startPosition: 92,
                    expectedLength: 7);
            }

            var assignments = orderedBatches
                .Select((batch, index) => new { batch.Id, Number = index + 1 })
                .ToDictionary(item => item.Id, item => item.Number);
            return Task.FromResult(new BatchNumberAssignmentResult(
                assignments,
                "ACHCOL_FILE_LOCAL_ORDINAL",
                1,
                [new BatchNumberScopeTrace("ACHCOL_FILE_LOCAL_ORDINAL", "CURRENT_FILE", 0, orderedBatches.Count, true, orderedBatches.Count)]));
        }

        var hasCompletePersistedAssignment = orderedBatches.Count > 0
            && orderedBatches.All(batch => batch.BatchSequenceNumber > 0)
            && orderedBatches
                .GroupBy(batch => (batch.OriginOrOdfi ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .All(group => group.Select(batch => batch.BatchSequenceNumber).Distinct().Count() == group.Count());

        if (!hasCompletePersistedAssignment)
        {
            return _batchNumberGenerator.AssignBatchNumbersAsync(orderedBatches, clearingHouseCode, processingDate, ct);
        }

        return Task.FromResult(new BatchNumberAssignmentResult(
            orderedBatches.ToDictionary(batch => batch.Id, batch => batch.BatchSequenceNumber),
            "PERSISTED_BATCH_SEQUENCE",
            orderedBatches.Select(batch => (batch.OriginOrOdfi ?? string.Empty).Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            []));
    }

    private async Task<IReadOnlyList<EntryDetailRecord>> BuildEntryDetailRecordsOfficialAsync(
        IReadOnlyList<AchTransaction> transactions,
        CfgLayoutVariant record6Layout,
        IReadOnlyDictionary<int, List<NachaType7RecordCandidate>> type7ByTransaction,
        CancellationToken ct)
    {
        var records = new List<EntryDetailRecord>(transactions.Count);
        var receivingDfiLength = ResolveOfficialFieldLength(record6Layout, "RECEIVINGDFI");
        var receiverLookup = await BuildReceiverLookupAsync(transactions, ct);

        foreach (var transaction in transactions.OrderBy(t => t.Id))
        {
            var receiverName = await ResolveReceiverNameForType6Async(transaction, receiverLookup, ct);
            if (string.IsNullOrWhiteSpace(receiverName))
            {
                throw new NachaGenerationException(
                    "NACHA_FIELD_RULE_FAILED",
                    "El nombre del receptor es obligatorio para el registro tipo 6.",
                    ruleId: "ACHCOL-T6-INDIVIDUAL-NAME",
                    chamber: "ACHCOL",
                    recordType: "6",
                    fieldName: "INDIVIDUALNAME",
                    cause: "Valor requerido ausente.",
                    startPosition: 63,
                    expectedLength: 22);
            }

            records.Add(EntryDetailRecord.From(
                transaction,
                receiverName,
                receivingDfiLength,
                type7ByTransaction.TryGetValue(transaction.Id, out var addendas) && addendas.Count > 0));
        }

        return records;
    }

    private static CfgLayoutVariant ResolveType7Layout(
        NachaConfigResolutionResult resolution,
        NachaType7RecordCandidate candidate)
    {
        if (!resolution.LayoutVariantsByRecordCode.TryGetValue("7", out var variants) || variants.Count == 0)
        {
            throw new NachaGenerationException(
                "NACHA_REQUIRED_RECORD_MISSING",
                "Falta layout publicado para el registro tipo 7 requerido por una entrada con adenda.",
                ruleId: "ACHCOL-T7-ADDENDA-TYPE",
                chamber: "ACHCOL",
                recordType: "7",
                fieldName: "ADDENDATYPE",
                cause: "Variante de adenda requerida ausente.",
                startPosition: 2,
                expectedLength: 2);
        }

        string[] prenotificationCodes = ["23", "28", "33", "38", "53", "57"];
        var transactionCode = candidate.Transaction.TransactionCode?.Trim() ?? string.Empty;
        var creditVariantCode = prenotificationCodes.Contains(transactionCode, StringComparer.Ordinal)
            ? AchColOfficialNachaLayout.Type7CreditPrenotificationVariant
            : AchColOfficialNachaLayout.Type7CreditMonetaryVariant;
        var variantCode = candidate.Addenda.BusinessType switch
        {
            Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.AchAddendaBusinessType.Credit => variants
                .Select(variant => variant.VariantCode)
                .SingleOrDefault(code => string.Equals(code, creditVariantCode, StringComparison.OrdinalIgnoreCase)) ?? string.Empty,
            Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.AchAddendaBusinessType.Debit => AchColOfficialNachaLayout.Type7DebitVariant,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(variantCode)
            || variants.FirstOrDefault(variant => string.Equals(variant.VariantCode, variantCode, StringComparison.OrdinalIgnoreCase)) is not { } selected)
        {
            throw BuildCrossFieldException(
                "ACHCOL-T7-ADDENDA-TYPE",
                "7",
                "ADDENDATYPE",
                2,
                2,
                "La variante de adenda no está demostrada o publicada para ACHCOL.");
        }

        return selected;
    }

    private static void ValidateType7Association(EntryDetailRecord entry, NachaType7RecordCandidate candidate)
    {
        if (entry.AddendumIndicator != "1")
        {
            throw BuildCrossFieldException(
                "ACHCOL-T6-ADDENDA-INDICATOR",
                "6/7",
                "ADDENDARECORDINDICATOR",
                87,
                1,
                "Existe T7, pero el T6 asociado no declara adenda.");
        }

        var trace = entry.TraceNumber?.Trim() ?? string.Empty;
        if (trace.Length != 15 || trace.Any(character => !char.IsDigit(character)))
        {
            throw BuildCrossFieldException(
                "ACHCOL-T6-TRACE-NUMBER",
                "6",
                "TRACENUMBER",
                88,
                15,
                "Trace Number debe contener exactamente quince dígitos.");
        }

        var suffix = candidate.FieldValues.TryGetValue("TraceSuffix", out var rawSuffix)
            ? Convert.ToString(rawSuffix, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;
        if (!string.Equals(suffix, trace[^7..], StringComparison.Ordinal))
        {
            throw BuildCrossFieldException(
                "ACHCOL-T7-TRACE-SUFFIX-MATCH",
                "6/7",
                "TRACESUFFIX",
                88,
                7,
                "El sufijo T7 no coincide con el Trace Number del T6 asociado.");
        }
    }

    private static NachaGenerationException BuildCrossFieldException(
        string ruleId,
        string recordType,
        string fieldName,
        int startPosition,
        int expectedLength,
        string cause)
        => new(
            "NACHA_CROSS_FIELD_VALIDATION_FAILED",
            "Falló una validación cruzada NACHA-M.",
            ruleId,
            "ACHCOL",
            recordType,
            fieldName,
            cause,
            startPosition,
            expectedLength);

    private static int ResolveOfficialFieldLength(CfgLayoutVariant layout, string fieldCode)
    {
        var field = layout.Fields.FirstOrDefault(x =>
            x.IsEnabled &&
            string.Equals(x.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));
        if (field is null)
        {
            throw new NachaGenerationException("NACHA_REQUIRED_FIELD_MISSING", $"Falta el campo requerido {fieldCode} en RecordCode={layout.RecordCode?.Code}.");
        }

        return field.Length;
    }

    private static string ResolveMassCreditDescription(CfgLayoutVariant layout)
    {
        const string normativeDescription = "MULTICREDIT";
        var configuredLength = ResolveOfficialFieldLength(layout, "COMPANYENTRYDESCRIPTION");
        return normativeDescription.Length <= configuredLength
            ? normativeDescription
            : normativeDescription[..configuredLength];
    }

    private static string ResolveEntryHashSourceFieldPath(CfgLayoutVariant record6Layout)
    {
        var receivingDfiField = record6Layout.Fields.FirstOrDefault(x =>
            x.IsEnabled && string.Equals(x.FieldCode, "RECEIVINGDFI", StringComparison.OrdinalIgnoreCase));
        var source = receivingDfiField?.SourceDefinition?.PropertyPath;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new NachaGenerationException("NACHA_ENTRY_HASH_SOURCE_MISSING", "Falta sourceFieldPath para RECEIVINGDFI, requerido para calcular EntryHash.");
        }

        return source;
    }

    private static int ResolveOfficialBlockSize(CfgLayoutVariant record1Layout)
    {
        var field = record1Layout.Fields.FirstOrDefault(x =>
            x.IsEnabled && string.Equals(x.FieldCode, "BLOCKINGFACTOR", StringComparison.OrdinalIgnoreCase));
        var configuredValue = field?.SourceDefinition?.ConstantValue;
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return 10;
        }

        if (!int.TryParse(configuredValue, NumberStyles.None, CultureInfo.InvariantCulture, out var blockSize) || blockSize <= 0)
        {
            throw new NachaGenerationException("NACHA_BLOCK_SIZE_INVALID", $"El blockSize configurado es inválido: {configuredValue}.");
        }

        return blockSize;
    }

    private static string BuildPaddingRecord(CfgLayoutVariant fileControlLayout)
    {
        var recordType = fileControlLayout.RecordCode?.Code;
        var paddingChar = string.Equals(recordType, "9", StringComparison.OrdinalIgnoreCase) ? '9' : '9';
        return new string(paddingChar, fileControlLayout.TotalLength);
    }

    private static void AddControlTotalsAudit(NachaGenerationAuditResult audit, NachaControlTotalsResult totals)
    {
        audit.FileTotals = new NachaFileControlTotalsAudit
        {
            BatchCount = totals.FileTotals.BatchCount,
            BlockCount = totals.FileTotals.BlockCount,
            EntryAddendaCount = totals.FileTotals.EntryAddendaCount,
            EntryHash = totals.FileTotals.EntryHash,
            TotalDebitAmountInCents = totals.FileTotals.TotalDebitAmountInCents,
            TotalCreditAmountInCents = totals.FileTotals.TotalCreditAmountInCents,
            PhysicalRecordCountBeforePadding = totals.FileTotals.PhysicalRecordCountBeforePadding,
            PaddingRecordCount = totals.FileTotals.PaddingRecordCount,
            PhysicalRecordCountAfterPadding = totals.FileTotals.PhysicalRecordCountAfterPadding
        };

        audit.BatchTotals.AddRange(totals.BatchTotals.Select(x => new NachaBatchControlTotalsAudit
        {
            BatchId = x.BatchId,
            EntryAddendaCount = x.EntryAddendaCount,
            EntryHash = x.EntryHash,
            TotalDebitAmountInCents = x.TotalDebitAmountInCents,
            TotalCreditAmountInCents = x.TotalCreditAmountInCents,
            EntryDetailCount = x.EntryDetailCount,
            AddendaCount = x.AddendaCount
        }));
    }

    private static void ValidateRenderedControlTotals(
        NachaGenerationAuditResult audit,
        string recordCode,
        int lineNumber,
        CfgLayoutVariant layout,
        NachaBatchControlTotals totals)
    {
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "ENTRYADDENDACOUNT", totals.EntryAddendaCount);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "ENTRYHASH", totals.EntryHash);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "TOTALDEBITAMOUNT", totals.TotalDebitAmountInCents);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "TOTALCREDITAMOUNT", totals.TotalCreditAmountInCents);
    }

    private static void ValidateRenderedControlTotals(
        NachaGenerationAuditResult audit,
        string recordCode,
        int lineNumber,
        CfgLayoutVariant layout,
        NachaFileControlTotals totals)
    {
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "BATCHCOUNT", totals.BatchCount);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "BLOCKCOUNT", totals.BlockCount);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "ENTRYADDENDACOUNT", totals.EntryAddendaCount);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "ENTRYHASH", totals.EntryHash);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "TOTALDEBITAMOUNT", totals.TotalDebitAmountInCents);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "TOTALCREDITAMOUNT", totals.TotalCreditAmountInCents);
    }

    private static void ValidateRenderedControlField(
        NachaGenerationAuditResult audit,
        string recordCode,
        int lineNumber,
        CfgLayoutVariant layout,
        string fieldCode,
        long calculatedValue)
    {
        var field = layout.Fields.First(x => x.IsEnabled && string.Equals(x.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));
        var expected = calculatedValue.ToString(CultureInfo.InvariantCulture);
        expected = field.Justification == 'R'
            ? expected.PadLeft(field.Length, field.PadChar)
            : expected.PadRight(field.Length, field.PadChar);
        var trace = audit.FieldTraceEntries.LastOrDefault(x =>
            x.LineNumber == lineNumber &&
            string.Equals(x.RecordType, recordCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.FieldName, fieldCode, StringComparison.OrdinalIgnoreCase));

        if (trace is null || !string.Equals(trace.RuntimeRenderedValue, expected, StringComparison.Ordinal))
        {
            throw new NachaGenerationException(
                "NACHA_CONTROL_TOTAL_MISMATCH",
                $"El valor renderizado de {recordCode}.{fieldCode} no coincide con el calculado.");
        }
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
                    throw new InvalidOperationException("Rollout policy bloqueó fallback type7 para un candidato; el identificador financiero fue omitido.");
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
            audit.Trace.Add($"R7:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:FB={trace.FallbackStrategy};VALUES=REDACTED");
        }

        if (!mapped.Success && mapped.ValuesByFieldCode.Count == 0)
        {
            audit.Warnings.Add("Type7 mapping engine devolvió fallback; identificadores y valores omitidos.");
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

    private bool IsOfficialTableDrivenMode()
        => string.Equals((_generationOptions.Mode ?? "TABLE_DRIVEN").Trim(), "TABLE_DRIVEN", StringComparison.OrdinalIgnoreCase);

    private async Task<NachaConfigResolutionResult> ResolveOfficialRuntimeConfigAsync(
        NachaBuildContext context,
        string clearingHouseCode,
        IReadOnlyCollection<string> recordCodes,
        CancellationToken ct)
    {
        if (_configResolver is null)
        {
            throw new NachaGenerationException("NACHA_PROFILE_NOT_PUBLISHED", "Resolver NACHA table-driven no está registrado. No se habilita fallback legacy.");
        }

        var request = BuildConfigResolutionRequest(context, clearingHouseCode, recordCodes);
        var resolution = await _configResolver.ResolveAsync(request, ct);
        if (resolution.SelectionStatus == NachaProfileSelectionStatus.ProfileAmbiguous
            || resolution.Warnings.Any(x => x.Contains("Ambig", StringComparison.OrdinalIgnoreCase)))
        {
            throw new NachaGenerationException("NACHA_PROFILE_AMBIGUOUS", string.Join(" | ", resolution.Warnings));
        }

        if (!resolution.Success || resolution.Profile is null)
        {
            if (resolution.Profile is not null
                && resolution.SelectionStatus == NachaProfileSelectionStatus.ProfileNotFound
                && resolution.Warnings.Any(x => x.Contains("RecordCode=", StringComparison.OrdinalIgnoreCase)))
            {
                throw new NachaGenerationException(
                    "NACHA_REQUIRED_RECORD_MISSING",
                    string.Join(" | ", resolution.Warnings));
            }

            var code = await ResolveProfileFailureCodeAsync(request, ct);
            throw new NachaGenerationException(code, $"No existe perfil NACHA-M publicado/vigente para {request.ClearingHouseCode}/{request.FlowTypeCode}/{request.DirectionCode}.");
        }

        if (resolution.UsedFallback)
        {
            var missing = recordCodes.Where(code => !resolution.LayoutsByRecordCode.ContainsKey(code)).ToArray();
            if (missing.Length > 0)
            {
                throw new NachaGenerationException("NACHA_REQUIRED_RECORD_MISSING", $"Faltan layout variants publicados para RecordCode={string.Join(",", missing)}.");
            }

            throw new NachaGenerationException("NACHA_LEGACY_GENERATION_DISABLED", "El resolver solicitó fallback legacy en modo oficial table-driven.");
        }

        EnforceCenitHomologationGate(request.ClearingHouseCode, resolution.Profile);

        return resolution;
    }

    private void EnforceCenitHomologationGate(string clearingHouseCode, CfgProfile profile)
    {
        if (!string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var executionScope = (_generationOptions.ExecutionScope ?? "LIVE").Trim();
        var isLive = string.Equals(executionScope, "LIVE", StringComparison.OrdinalIgnoreCase);
        var isPlaceholder = ReadProfileFlag(profile, "IsPlaceholder", defaultValue: true);
        var isHomologated = ReadProfileFlag(profile, "IsHomologated", defaultValue: false);
        var hasNormativeVersion = profile.Tags.Any(tag =>
            string.Equals(tag.TagKey, "NormativeVersion", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(tag.TagValue)
            && !tag.TagValue.Contains("NOT-DEMONSTRATED", StringComparison.OrdinalIgnoreCase));

        if (isLive || isPlaceholder || !isHomologated || !hasNormativeVersion)
        {
            if (!isLive && _generationOptions.AllowNonHomologatedCenitDevelopment)
            {
                return;
            }

            throw new NachaGenerationException(
                "CENIT_NOT_HOMOLOGATED",
                "CENIT permanece NO-GO / NOT HOMOLOGATED / BLOCKED FOR LIVE. Falta especificación técnica oficial y homologación explícita.",
                ruleId: "CENIT-FORMAT-NACHAM",
                chamber: "CENIT",
                recordType: "FILE",
                fieldName: "PROFILE",
                cause: "Perfil no homologado para el alcance solicitado.");
        }
    }

    private void EnforceCenitLiveGenerationGate(string clearingHouseCode)
    {
        if (!string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_generationOptions.ExecutionScope?.Trim(), "LIVE", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new NachaGenerationException(
            "CENIT_NOT_HOMOLOGATED",
            "CENIT permanece NO-GO / NOT HOMOLOGATED / BLOCKED FOR LIVE.",
            ruleId: "CENIT-FORMAT-NACHAM",
            chamber: "CENIT",
            recordType: "FILE",
            fieldName: "PROFILE",
            cause: "Generación LIVE bloqueada antes de seleccionar motor o perfil.");
    }

    private void EnforceLiveOfficialMode(string clearingHouseCode)
    {
        if (!string.Equals(_generationOptions.ExecutionScope?.Trim(), "LIVE", StringComparison.OrdinalIgnoreCase)
            || IsOfficialTableDrivenMode())
        {
            return;
        }

        throw new NachaGenerationException(
            "NACHA_LIVE_OFFICIAL_MODE_REQUIRED",
            "La generación LIVE exige el motor oficial table-driven; HYBRID y legacy quedan aislados a desarrollo controlado.",
            ruleId: "ACHCOL-GENERATION-FAIL-CLOSED",
            chamber: clearingHouseCode,
            recordType: "FILE",
            fieldName: "GENERATION_MODE",
            cause: "Modo no oficial solicitado para alcance LIVE.");
    }

    private static bool ReadProfileFlag(CfgProfile profile, string key, bool defaultValue)
    {
        var value = profile.Tags.FirstOrDefault(tag =>
            string.Equals(tag.TagKey, key, StringComparison.OrdinalIgnoreCase))?.TagValue;
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private NachaConfigResolutionRequest BuildConfigResolutionRequest(
        NachaBuildContext context,
        string clearingHouseCode,
        IReadOnlyCollection<string> recordCodes)
    {
        var serviceClassCode = context.Batches
            .Select(x => x.ServiceClassCode)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var flowTypeCode = NachaProfileDimensionResolver.ResolveFlowCode(context.Transactions);
        var requiresAchColV35 = string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase)
                               && flowTypeCode is "ORIGINAL" or "PRENOTIFICACION";

        return new NachaConfigResolutionRequest
        {
            ClearingHouseCode = clearingHouseCode,
            FlowTypeCode = flowTypeCode,
            DirectionCode = NachaProfileDimensionResolver.ResolveDirectionCode(context.Transactions),
            ServiceClassCode = serviceClassCode,
            ProcessDateUtc = context.Cycle.ProcessingDate,
            RequestedVersionMajor = requiresAchColV35 ? AchColOfficialNachaLayout.ProfileVersionMajor : null,
            RequestedVersionMinor = requiresAchColV35 ? AchColOfficialNachaLayout.ProfileVersionMinor : null,
            RecordCodes = recordCodes.ToList(),
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CycleName"] = context.Cycle.CycleName ?? string.Empty,
                ["ClearingHouseId"] = context.Cycle.ClearingHouseId.ToString(CultureInfo.InvariantCulture)
            }
        };
    }

    private async Task<string> ResolveProfileFailureCodeAsync(NachaConfigResolutionRequest request, CancellationToken ct)
    {
        var date = request.ProcessDateUtc.Date;
        var profiles = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .Include(x => x.Status)
            .Where(x => x.ClearingHouse.Code == request.ClearingHouseCode
                        && x.FlowType.Code == request.FlowTypeCode
                        && x.Direction.Code == request.DirectionCode
                        && (x.ServiceClass == null || x.ServiceClass.Code == request.ServiceClassCode))
            .ToListAsync(ct);

        if (profiles.Any(x => string.Equals(x.Status.Code, "PUBLICADO", StringComparison.OrdinalIgnoreCase)
                              && (x.EffectiveFrom.Date > date || (x.EffectiveTo.HasValue && x.EffectiveTo.Value.Date < date))))
        {
            return "NACHA_PROFILE_NOT_EFFECTIVE";
        }

        return "NACHA_PROFILE_NOT_PUBLISHED";
    }

    private async Task<string> ResolveClearingHouseCodeAsync(NachaBuildContext context, CancellationToken ct)
    {
        var configuredProfileCode = await _context.ClearingHouseConfigs
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == context.Cycle.ClearingHouseId && x.NachaProfileId != null)
            .Select(x => x.NachaProfile!.ClearingHouse.Code)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(configuredProfileCode))
        {
            return configuredProfileCode;
        }

        var operationalCode = context.Cycle.ClearingHouse?.Code?.Trim();
        var operationalName = context.Cycle.ClearingHouse?.Name?.Trim();
        var candidates = await _context.CatClearingHouses
            .AsNoTracking()
            .Where(x => x.IsActive
                        && ((!string.IsNullOrEmpty(operationalCode) && x.Code == operationalCode)
                            || (!string.IsNullOrEmpty(operationalName) && x.Name == operationalName)))
            .Select(x => x.Code)
            .Distinct()
            .Take(2)
            .ToListAsync(ct);

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        throw new NachaGenerationException(
            candidates.Count == 0 ? "NACHA_CLEARING_HOUSE_PROFILE_NOT_CONFIGURED" : "NACHA_CLEARING_HOUSE_PROFILE_AMBIGUOUS",
            candidates.Count == 0
                ? "La cámara del ciclo no está asociada a un catálogo de perfiles NACHA-M."
                : "La cámara del ciclo coincide con más de un catálogo de perfiles NACHA-M.");
    }

    private static CfgLayoutVariant RequireOfficialLayout(NachaConfigResolutionResult resolution, string recordCode)
    {
        if (!resolution.LayoutsByRecordCode.TryGetValue(recordCode, out var layout))
        {
            throw new NachaGenerationException("NACHA_REQUIRED_RECORD_MISSING", $"Falta variant publicado para RecordCode={recordCode}.");
        }

        return layout;
    }

    private static void ValidateOfficialLayout(string recordCode, CfgLayoutVariant layout, NachaGenerationAuditResult? audit = null)
    {
        var enabledFields = layout.Fields.Where(x => x.IsEnabled).OrderBy(x => x.StartPosition).ToList();
        if (enabledFields.Count == 0)
        {
            throw new NachaGenerationException("NACHA_REQUIRED_FIELD_MISSING", $"RecordCode={recordCode} no tiene campos activos.");
        }

        var requiredFields = recordCode switch
        {
            "1" => new[] { "RECORDTYPE", "IMMEDIATEDESTINATION", "IMMEDIATEORIGIN", "FILECREATIONDATE", "FILECREATIONTIME", "FILEIDMODIFIER" },
            "5" => new[] { "RECORDTYPE", "SERVICECLASSCODE", "COMPANYNAME", "COMPANYIDENTIFICATION", "COMPANYENTRYDESCRIPTION", "BATCHNUMBER" },
            "6" => new[] { "RECORDTYPE", "TRANSACTIONCODE", "RECEIVINGDFI", "CHECKDIGIT", "DFIACCOUNTNUMBER", "AMOUNT", "TRACENUMBER" },
            "7" => Array.Empty<string>(),
            "8" => new[] { "RECORDTYPE", "SERVICECLASSCODE", "ENTRYADDENDACOUNT", "ENTRYHASH", "TOTALDEBITAMOUNT", "TOTALCREDITAMOUNT", "BATCHNUMBER" },
            "9" => new[] { "RECORDTYPE", "BATCHCOUNT", "BLOCKCOUNT", "ENTRYADDENDACOUNT", "ENTRYHASH", "TOTALDEBITAMOUNT", "TOTALCREDITAMOUNT" },
            _ => Array.Empty<string>()
        };

        foreach (var required in requiredFields)
        {
            var field = enabledFields.FirstOrDefault(x => string.Equals(x.FieldCode, required, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                audit?.FieldTraceEntries.Add(new NachaGenerationTraceEntry
                {
                    TraceId = audit.TraceId,
                    RecordType = recordCode,
                    FieldName = required,
                    DisplayName = required,
                    ValidationStatus = "Failed",
                    ErrorCode = "NACHA_REQUIRED_FIELD_MISSING",
                    ErrorMessage = $"Falta el campo requerido {required} en RecordCode={recordCode}."
                });
                throw new NachaGenerationException("NACHA_REQUIRED_FIELD_MISSING", $"Falta el campo requerido {required} en RecordCode={recordCode}.");
            }
        }

        var occupied = new HashSet<int>();
        foreach (var field in enabledFields)
        {
            if (field.Length <= 0 || field.StartPosition <= 0 || field.StartPosition + field.Length - 1 > layout.TotalLength)
            {
                throw new NachaGenerationException("NACHA_FIELD_VALIDATION_FAILED", $"Campo {field.FieldCode} tiene posición/longitud inválida en RecordCode={recordCode}.");
            }

            for (var position = field.StartPosition; position < field.StartPosition + field.Length; position++)
            {
                if (!occupied.Add(position))
                {
                    throw new NachaGenerationException("NACHA_FIELD_VALIDATION_FAILED", $"Campo {field.FieldCode} se solapa en RecordCode={recordCode}.");
                }
            }

            var source = field.SourceDefinition;
            var sourceType = source?.DataSourceType?.Code;
            if (source is null || string.IsNullOrWhiteSpace(sourceType))
            {
                throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"Campo {field.FieldCode} no tiene source definition.");
            }

            if (string.Equals(sourceType, "CONSTANTE", StringComparison.OrdinalIgnoreCase) && source.ConstantValue is null)
            {
                throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"Campo constante {field.FieldCode} no tiene valor.");
            }

            if (string.Equals(sourceType, "ENTIDAD", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(source.PropertyPath))
            {
                throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"Campo entidad {field.FieldCode} no tiene sourceFieldPath.");
            }

            if (string.Equals(sourceType, "EXPRESION", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(source.ExpressionDsl))
            {
                throw new NachaGenerationException("NACHA_CALCULATION_FAILED", $"Campo calculado {field.FieldCode} no tiene calculationType.");
            }
        }

        if (string.Equals(audit?.ClearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase))
        {
            ValidateAchColOfficialLayoutSnapshot(recordCode, layout);
        }
        else if (string.Equals(audit?.ClearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase)
                 && CenitReturnOut2026Layout.IsVariant(layout.VariantCode))
        {
            ValidateCenitReturnOut2026LayoutSnapshot(recordCode, layout);
        }
        else if (string.Equals(audit?.ClearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase)
                 && CenitOrdinaryOutbound2026Layout.IsVariant(layout.VariantCode))
        {
            ValidateCenitOrdinaryOutbound2026LayoutSnapshot(recordCode, layout);
        }
    }

    private static void ValidateCenitOrdinaryOutbound2026LayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
        if (layout.TotalLength != CenitOrdinaryOutbound2026Layout.RecordLength
            || !CenitOrdinaryOutbound2026Layout.IsVariant(layout.VariantCode))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado no coincide con el snapshot normativo CENIT ordinario 2026.");
        }

        var expectedFields = CenitOrdinaryOutbound2026Layout.ForRecord(recordCode);
        var actualFields = layout.Fields.Where(field => field.IsEnabled).ToList();
        foreach (var expected in expectedFields)
        {
            var actual = actualFields.FirstOrDefault(field =>
                string.Equals(field.FieldCode, expected.FieldCode, StringComparison.OrdinalIgnoreCase));
            if (actual is null
                || actual.StartPosition != expected.StartPosition
                || actual.Length != expected.Length
                || actual.Justification != expected.Justification
                || actual.PadChar != expected.PadChar
                || !string.Equals(actual.FormatMask, expected.Format, StringComparison.Ordinal)
                || !actual.Rules.Any(rule => rule.IsEnabled
                    && string.Equals(rule.RuleCode, expected.RuleId, StringComparison.OrdinalIgnoreCase)))
            {
                throw BuildFieldRuleException(
                    "NACHA_PROFILE_LAYOUT_MISMATCH",
                    expected,
                    "El campo no coincide con el Manual CENIT 2026, Anexo 1.");
            }

            if (ContainsForbiddenOfficialTransformation(actual.TransformationPipelineJson)
                || !string.IsNullOrWhiteSpace(actual.SourceDefinition?.FallbackPolicyJson))
            {
                throw BuildFieldRuleException(
                    "NACHA_SILENT_TRANSFORMATION_FORBIDDEN",
                    expected,
                    "La ruta oficial CENIT no admite truncamiento, substring ni fallback silencioso.");
            }
        }

        var expectedCodes = expectedFields.Select(field => field.FieldCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (actualFields.Any(field => !expectedCodes.Contains(field.FieldCode)))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado contiene campos no contemplados por el snapshot CENIT ordinario 2026.");
        }
    }

    private static void ValidateCenitReturnOut2026LayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
        if (layout.TotalLength != CenitReturnOut2026Layout.RecordLength
            || !CenitReturnOut2026Layout.IsVariant(layout.VariantCode))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado no coincide con el snapshot normativo CENIT 2026.");
        }

        var expectedFields = CenitReturnOut2026Layout.ForRecord(recordCode);
        var actualFields = layout.Fields.Where(field => field.IsEnabled).ToList();
        foreach (var expected in expectedFields)
        {
            var actual = actualFields.FirstOrDefault(field =>
                string.Equals(field.FieldCode, expected.FieldCode, StringComparison.OrdinalIgnoreCase));
            if (actual is null
                || actual.StartPosition != expected.StartPosition
                || actual.Length != expected.Length
                || actual.Justification != expected.Justification
                || actual.PadChar != expected.PadChar
                || !string.Equals(actual.FormatMask, expected.Format, StringComparison.Ordinal)
                || !actual.Rules.Any(rule => rule.IsEnabled
                    && string.Equals(rule.RuleCode, expected.RuleId, StringComparison.OrdinalIgnoreCase)))
            {
                throw BuildFieldRuleException(
                    "NACHA_PROFILE_LAYOUT_MISMATCH",
                    expected,
                    "El campo no coincide con el Manual CENIT 2026, sección 7.2.1 y Anexo 1.6.");
            }

            if (ContainsForbiddenOfficialTransformation(actual.TransformationPipelineJson)
                || !string.IsNullOrWhiteSpace(actual.SourceDefinition?.FallbackPolicyJson))
            {
                throw BuildFieldRuleException(
                    "NACHA_SILENT_TRANSFORMATION_FORBIDDEN",
                    expected,
                    "La ruta oficial CENIT no admite truncamiento, substring ni fallback silencioso.");
            }
        }

        var expectedCodes = expectedFields.Select(field => field.FieldCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (actualFields.Any(field => !expectedCodes.Contains(field.FieldCode)))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado contiene campos no contemplados por el snapshot CENIT 2026.");
        }
    }

    private static void ValidateAchColOfficialLayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
        if (layout.TotalLength != AchColOfficialNachaLayout.RecordLength)
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado no coincide con el snapshot normativo ACHCOL.",
                "ACHCOL-PHYSICAL-RECORD-LENGTH",
                "ACHCOL",
                recordCode,
                "RECORD",
                "Longitud física distinta de 106.",
                1,
                AchColOfficialNachaLayout.RecordLength);
        }

        var expectedFields = AchColReturnOutV35Layout.IsVariant(layout.VariantCode)
            ? AchColReturnOutV35Layout.ForRecord(recordCode)
            : AchColOfficialNachaLayout.ForVariant(recordCode, layout.VariantCode);
        var actualFields = layout.Fields.Where(field => field.IsEnabled).ToList();
        foreach (var expected in expectedFields)
        {
            var actual = actualFields.FirstOrDefault(field =>
                string.Equals(field.FieldCode, expected.FieldCode, StringComparison.OrdinalIgnoreCase));
            if (actual is null
                || actual.StartPosition != expected.StartPosition
                || actual.Length != expected.Length
                || actual.Justification != expected.Justification
                || actual.PadChar != expected.PadChar
                || !string.Equals(actual.FormatMask, expected.Format, StringComparison.Ordinal))
            {
                throw BuildFieldRuleException(
                    "NACHA_PROFILE_LAYOUT_MISMATCH",
                    expected,
                    "Posición, longitud, alineación, relleno o formato no coincide con MAN-004 V35.");
            }

            if (!actual.Rules.Any(rule => rule.IsEnabled && string.Equals(rule.RuleCode, expected.RuleId, StringComparison.OrdinalIgnoreCase)))
            {
                throw BuildFieldRuleException(
                    "NACHA_REQUIRED_RULE_MISSING",
                    expected,
                    "CfgFieldRule ejecutable ausente para el campo crítico.");
            }

            if (ContainsForbiddenOfficialTransformation(actual.TransformationPipelineJson)
                || !string.IsNullOrWhiteSpace(actual.SourceDefinition?.FallbackPolicyJson))
            {
                throw BuildFieldRuleException(
                    "NACHA_SILENT_TRANSFORMATION_FORBIDDEN",
                    expected,
                    "La ruta oficial no admite truncamiento, substring ni fallback silencioso.");
            }
        }

        var expectedCodes = expectedFields.Select(field => field.FieldCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unexpected = actualFields.FirstOrDefault(field => !expectedCodes.Contains(field.FieldCode));
        if (unexpected is not null)
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado contiene un campo no contemplado por el snapshot ACHCOL.",
                "ACHCOL-PHYSICAL-RECORD-LENGTH",
                "ACHCOL",
                recordCode,
                unexpected.FieldCode,
                "Campo habilitado no reconocido.",
                unexpected.StartPosition,
                unexpected.Length);
        }
    }

    private static bool ContainsForbiddenOfficialTransformation(string? pipelineJson)
        => !string.IsNullOrWhiteSpace(pipelineJson)
           && (pipelineJson.Contains("truncate", StringComparison.OrdinalIgnoreCase)
               || pipelineJson.Contains("substring", StringComparison.OrdinalIgnoreCase));

    private string RenderOfficialRecord(
        string recordCode,
        object record,
        CfgLayoutVariant layout,
        NachaGenerationAuditResult audit,
        int recordSequence,
        int lineNumber)
    {
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');
        var recordEntries = new List<NachaGenerationTraceEntry>();

        foreach (var field in layout.Fields.Where(x => x.IsEnabled).OrderBy(x => x.StartPosition))
        {
            object? raw = null;
            string? value = null;
            try
            {
                raw = ResolveOfficialRawValue(recordCode, record, field);
                value = FormatOfficialValue(raw, field);
                value = ExecuteOfficialFieldRules(recordCode, field, value, audit.ClearingHouseCode);

                value = field.Justification == 'R'
                    ? value.PadLeft(field.Length, field.PadChar)
                    : value.PadRight(field.Length, field.PadChar);

                ValidateRenderedOfficialField(recordCode, field, value, audit.ClearingHouseCode);

                value.CopyTo(0, buffer, field.StartPosition - 1, value.Length);
                recordEntries.Add(CreateOfficialTraceEntry(audit, recordCode, recordSequence, lineNumber, layout, field, raw, value, "Ok", null, null));
            }
            catch (NachaGenerationException ex)
            {
                audit.FieldTraceEntries.Add(CreateOfficialTraceEntry(audit, recordCode, recordSequence, lineNumber, layout, field, raw, value, "Failed", ex.Code, ex.Message));
                throw;
            }
        }

        var line = new string(buffer);
        var lineEvidence = BuildSafeRecordEvidence(recordCode, line);
        foreach (var entry in recordEntries)
        {
            entry.GeneratedLinePreviewSanitized = lineEvidence;
            audit.FieldTraceEntries.Add(entry);
        }

        return line;
    }

    private static string ExecuteOfficialFieldRules(
        string recordCode,
        CfgLayoutField field,
        string value,
        string? clearingHouseCode)
    {
        var rules = field.Rules.Where(rule => rule.IsEnabled).OrderBy(rule => rule.Order).ToList();
        var isCenitOrdinary = CenitOrdinaryOutbound2026Layout.IsVariant(field.LayoutVariant?.VariantCode);
        if ((string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase)
             || (string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase)
                 && (CenitReturnOut2026Layout.IsVariant(field.LayoutVariant?.VariantCode) || isCenitOrdinary)))
            && rules.Count == 0)
        {
            var descriptor = isCenitOrdinary
                ? CenitOrdinaryOutbound2026Layout.Field(recordCode, field.FieldCode)
                : CenitReturnOut2026Layout.IsVariant(field.LayoutVariant?.VariantCode)
                ? CenitReturnOut2026Layout.Field(recordCode, field.FieldCode)
                : AchColReturnOutV35Layout.IsVariant(field.LayoutVariant?.VariantCode)
                ? AchColReturnOutV35Layout.Field(recordCode, field.FieldCode)
                : AchColOfficialNachaLayout.Field(recordCode, field.FieldCode, field.LayoutVariant?.VariantCode);
            throw BuildFieldRuleException("NACHA_REQUIRED_RULE_MISSING", descriptor, "CfgFieldRule ejecutable ausente.");
        }

        var current = value;
        foreach (var rule in rules)
        {
            FieldRuleRuntimeConfig config;
            try
            {
                config = ParseFieldRuleRuntimeConfig(rule, field, recordCode);
            }
            catch (JsonException)
            {
                throw BuildRuleException(
                    "NACHA_RULE_CONFIG_INVALID",
                    rule.RuleCode,
                    clearingHouseCode,
                    recordCode,
                    field,
                    "RuleConfigJson no es válido.");
            }

            current = config.Normalizer switch
            {
                "NONE" => current,
                "TRIM" => current.Trim(),
                "UPPER" => current.ToUpperInvariant(),
                "TRIM_UPPER" => current.Trim().ToUpperInvariant(),
                _ => throw BuildRuleException(
                    "NACHA_NORMALIZER_NOT_ALLOWED",
                    rule.RuleCode,
                    clearingHouseCode,
                    recordCode,
                    field,
                    "Normalizador no autorizado por el motor oficial.")
            };

            var isBlank = string.IsNullOrWhiteSpace(current);
            if (config.Required && isBlank)
            {
                throw BuildRuleException("NACHA_REQUIRED_FIELD_MISSING", rule.RuleCode, clearingHouseCode, recordCode, field, "Valor obligatorio ausente.");
            }

            if (!isBlank)
            {
                ValidateOfficialDataType(config, current, rule.RuleCode, clearingHouseCode, recordCode, field);
                ValidateAllowedRepertoire(config, current, rule.RuleCode, clearingHouseCode, recordCode, field);

                if (config.AllowedValues.Count > 0 && !config.AllowedValues.Contains(current, StringComparer.OrdinalIgnoreCase))
                {
                    throw BuildRuleException("NACHA_ALLOWED_VALUE_INVALID", rule.RuleCode, clearingHouseCode, recordCode, field, "Valor fuera del catálogo permitido.");
                }
            }

            if (current.Length > field.Length)
            {
                throw BuildRuleException("NACHA_FIELD_LENGTH_INVALID", rule.RuleCode, clearingHouseCode, recordCode, field, "Overflow; la política oficial es REJECT.");
            }

            if (!string.Equals(config.OverflowPolicy, "REJECT", StringComparison.OrdinalIgnoreCase))
            {
                throw BuildRuleException("NACHA_OVERFLOW_POLICY_INVALID", rule.RuleCode, clearingHouseCode, recordCode, field, "La ruta oficial exige overflowPolicy=REJECT.");
            }
        }

        return current;
    }

    private static void ValidateOfficialDataType(
        FieldRuleRuntimeConfig config,
        string value,
        string ruleId,
        string? clearingHouseCode,
        string recordCode,
        CfgLayoutField field)
    {
        var valid = config.DataType switch
        {
            "NUMERIC" => value.All(char.IsDigit),
            "DATE" => DateTime.TryParseExact(value, config.Format ?? "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "TIME" => DateTime.TryParseExact(value, config.Format ?? "HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "RESERVED" => value.All(char.IsWhiteSpace),
            "ALPHANUMERIC" => true,
            _ => false
        };

        if (!valid)
        {
            throw BuildRuleException("NACHA_FIELD_TYPE_INVALID", ruleId, clearingHouseCode, recordCode, field, "Tipo o formato del campo inválido.");
        }
    }

    private static void ValidateAllowedRepertoire(
        FieldRuleRuntimeConfig config,
        string value,
        string ruleId,
        string? clearingHouseCode,
        string recordCode,
        CfgLayoutField field)
    {
        if (config.DataType is "NUMERIC" or "DATE" or "TIME" or "RESERVED")
        {
            return;
        }

        const string permittedSpecials = " .,:;-*/&#$%=";
        if (value.Any(character => char.IsControl(character)
                                   || character > 0x7F
                                   || (!(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')
                                       && !permittedSpecials.Contains(character))))
        {
            throw BuildRuleException("NACHA_CHARACTER_REPERTOIRE_INVALID", ruleId, clearingHouseCode, recordCode, field, "El campo contiene caracteres fuera del repertorio permitido.");
        }
    }

    private static void ValidateRenderedOfficialField(
        string recordCode,
        CfgLayoutField field,
        string rendered,
        string? clearingHouseCode)
    {
        if (rendered.Length != field.Length)
        {
            throw BuildRuleException("NACHA_RENDERED_LENGTH_INVALID", ResolveRuleId(field), clearingHouseCode, recordCode, field, "Longitud renderizada distinta de la longitud configurada.");
        }

        if (field.Justification == 'R'
            && field.PadChar == '0'
            && !string.IsNullOrWhiteSpace(rendered)
            && rendered.Any(character => !char.IsDigit(character)))
        {
            throw BuildRuleException("NACHA_NUMERIC_RENDER_INVALID", ResolveRuleId(field), clearingHouseCode, recordCode, field, "Campo numérico renderizado contiene caracteres no numéricos.");
        }
    }

    private static FieldRuleRuntimeConfig ParseFieldRuleRuntimeConfig(CfgFieldRule rule, CfgLayoutField field, string recordCode)
    {
        using var document = JsonDocument.Parse(rule.RuleConfigJson ?? "{}");
        var root = document.RootElement;
        var configuredRecord = ReadJsonString(root, "recordType");
        var configuredField = ReadJsonString(root, "field");
        var configuredStart = root.TryGetProperty("startPosition", out var start) ? start.GetInt32() : 0;
        var configuredLength = root.TryGetProperty("length", out var length) ? length.GetInt32() : 0;
        if (!string.Equals(configuredRecord, recordCode, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(configuredField, field.FieldCode, StringComparison.OrdinalIgnoreCase)
            || configuredStart != field.StartPosition
            || configuredLength != field.Length)
        {
            throw BuildRuleException("NACHA_RULE_METADATA_MISMATCH", rule.RuleCode, "ACHCOL", recordCode, field, "La metadata de CfgFieldRule no corresponde al descriptor renderizado.");
        }

        var allowedValues = root.TryGetProperty("allowedValues", out var allowed) && allowed.ValueKind == JsonValueKind.Array
            ? allowed.EnumerateArray().Select(item => item.GetString()).Where(item => item is not null).Cast<string>().ToArray()
            : Array.Empty<string>();

        return new FieldRuleRuntimeConfig(
            ReadJsonString(root, "dataType").ToUpperInvariant(),
            root.TryGetProperty("required", out var required) && required.GetBoolean(),
            ReadJsonString(root, "normalizer", "NONE").ToUpperInvariant(),
            ReadJsonString(root, "overflowPolicy", "REJECT").ToUpperInvariant(),
            ReadJsonString(root, "format", null),
            allowedValues);
    }

    private static string ReadJsonString(JsonElement root, string propertyName, string? defaultValue = "")
        => root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? defaultValue ?? string.Empty
            : defaultValue ?? string.Empty;

    private static string ResolveRuleId(CfgLayoutField field)
        => field.Rules.FirstOrDefault(rule => rule.IsEnabled)?.RuleCode ?? "NACHA-RULE-METADATA";

    private static NachaGenerationException BuildRuleException(
        string code,
        string ruleId,
        string? clearingHouseCode,
        string recordCode,
        CfgLayoutField field,
        string cause)
        => new(
            code,
            "El campo no cumple la política oficial NACHA-M.",
            ruleId,
            string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase) ? "ACHCOL" : clearingHouseCode,
            recordCode,
            field.FieldCode,
            cause,
            field.StartPosition,
            field.Length);

    private static NachaGenerationException BuildFieldRuleException(
        string code,
        AchColOfficialFieldDescriptor descriptor,
        string cause)
        => new(
            code,
            "El perfil no cumple el descriptor oficial ACHCOL.",
            descriptor.RuleId,
            "ACHCOL",
            descriptor.RecordCode,
            descriptor.FieldCode,
            cause,
            descriptor.StartPosition,
            descriptor.Length);

    private static string BuildSafeRecordEvidence(string recordCode, string line)
        => $"RecordType={recordCode};Length={line.Length};SHA256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(line)))[..16]}";

    private sealed record FieldRuleRuntimeConfig(
        string DataType,
        bool Required,
        string Normalizer,
        string OverflowPolicy,
        string? Format,
        IReadOnlyList<string> AllowedValues);

    private static NachaGenerationTraceEntry CreateOfficialTraceEntry(
        NachaGenerationAuditResult audit,
        string recordCode,
        int recordSequence,
        int lineNumber,
        CfgLayoutVariant layout,
        CfgLayoutField field,
        object? raw,
        string? rendered,
        string validationStatus,
        string? errorCode,
        string? errorMessage)
    {
        var source = field.SourceDefinition;
        var sourceType = NormalizeTraceSourceType(source.DataSourceType.Code);
        var calculationType = string.Equals(source.DataSourceType.Code, "EXPRESION", StringComparison.OrdinalIgnoreCase)
            ? TryResolveCalculationType(source.ExpressionDsl)
            : null;

        return new NachaGenerationTraceEntry
        {
            TraceId = audit.TraceId,
            RecordType = recordCode,
            RecordSequence = recordSequence,
            LineNumber = lineNumber,
            LayoutVariantId = layout.Id,
            LayoutVariantCode = layout.VariantCode,
            FieldDefinitionId = field.Id,
            FieldName = field.FieldCode,
            DisplayName = field.FieldNameEs,
            PositionStart = field.StartPosition,
            PositionEnd = field.StartPosition + field.Length - 1,
            Length = field.Length,
            DataType = ResolveConfiguredTraceDataType(field, raw),
            Required = ResolveConfiguredRequired(field),
            SourceType = sourceType,
            SourceFieldPath = source.PropertyPath,
            ConstantValueSanitized = source.ConstantValue is null ? null : SanitizeTraceValue(source.ConstantValue, field),
            CalculationType = calculationType,
            TransformationApplied = field.FormatMask,
            PaddingDirection = field.Justification == 'R' ? "Left" : "Right",
            PaddingChar = field.PadChar.ToString(),
            RawValueSanitized = SanitizeTraceValue(raw, field),
            RenderedValue = SanitizeTraceValue(rendered, field),
            RuntimeRenderedValue = rendered,
            RenderedLength = rendered?.Length ?? 0,
            ValidationStatus = validationStatus,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ValueStartIndex = field.StartPosition,
            ValueEndIndex = field.StartPosition + field.Length - 1
        };
    }

    private static string NormalizeTraceSourceType(string sourceType)
    {
        if (string.Equals(sourceType, "CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            return "CONSTANT";
        }

        if (string.Equals(sourceType, "ENTIDAD", StringComparison.OrdinalIgnoreCase))
        {
            return "SOURCE_FIELD";
        }

        if (string.Equals(sourceType, "EXPRESION", StringComparison.OrdinalIgnoreCase))
        {
            return "CALCULATED";
        }

        return sourceType.ToUpperInvariant();
    }

    private static string InferTraceDataType(CfgLayoutField field, object? raw)
    {
        if (raw is DateTime or DateOnly)
        {
            return "date";
        }

        if (raw is decimal or double or float)
        {
            return "decimal";
        }

        if (raw is int or long or short)
        {
            return "integer";
        }

        return field.FieldCode.Contains("AMOUNT", StringComparison.OrdinalIgnoreCase)
            || field.FieldCode.Contains("TOTAL", StringComparison.OrdinalIgnoreCase)
            || field.FieldCode.Contains("COUNT", StringComparison.OrdinalIgnoreCase)
            || field.FieldCode.Contains("HASH", StringComparison.OrdinalIgnoreCase)
            ? "numeric"
            : "string";
    }

    private static string ResolveConfiguredTraceDataType(CfgLayoutField field, object? raw)
    {
        var configured = ReadRuleConfigString(field, "dataType");
        return string.IsNullOrWhiteSpace(configured)
            ? InferTraceDataType(field, raw)
            : configured.ToLowerInvariant();
    }

    private static bool ResolveConfiguredRequired(CfgLayoutField field)
    {
        foreach (var rule in field.Rules.Where(rule => rule.IsEnabled))
        {
            try
            {
                using var document = JsonDocument.Parse(rule.RuleConfigJson ?? "{}");
                if (document.RootElement.TryGetProperty("required", out var required)
                    && required.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return required.GetBoolean();
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }

        return false;
    }

    private static string ReadRuleConfigString(CfgLayoutField field, string propertyName)
    {
        foreach (var rule in field.Rules.Where(rule => rule.IsEnabled))
        {
            try
            {
                using var document = JsonDocument.Parse(rule.RuleConfigJson ?? "{}");
                if (document.RootElement.TryGetProperty(propertyName, out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    private static string? SanitizeTraceValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var text = value switch
        {
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        } ?? string.Empty;

        text = text.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

        if (text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Authorization", StringComparison.OrdinalIgnoreCase)
            || text.Contains("BEGIN PRIVATE", StringComparison.OrdinalIgnoreCase)
            || text.Contains("password", StringComparison.OrdinalIgnoreCase)
            || text.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            return "[SANITIZED]";
        }

        return text.Length <= 140 ? text : text[..140];
    }

    private static string? SanitizeTraceValue(object? value, CfgLayoutField field)
    {
        if (value is null)
        {
            return null;
        }

        var sensitivity = ReadRuleConfigString(field, "sensitivity");
        if (string.IsNullOrWhiteSpace(sensitivity) && IsSensitiveNachaField(field.FieldCode))
        {
            sensitivity = "FINANCIAL_OR_PERSONAL";
        }

        if (!string.IsNullOrWhiteSpace(sensitivity)
            && !string.Equals(sensitivity, "NONE", StringComparison.OrdinalIgnoreCase))
        {
            var length = Convert.ToString(value, CultureInfo.InvariantCulture)?.Length ?? 0;
            return $"[REDACTED;Category={sensitivity.ToUpperInvariant()};Length={length}]";
        }

        return SanitizeTraceValue(value);
    }

    private static bool IsSensitiveNachaField(string fieldCode)
    {
        var normalized = fieldCode.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return new[]
        {
            "ACCOUNT", "NAME", "IDENTIFICATION", "AMOUNT", "TRACE", "REFERENCE", "CUSTOMER", "CLIENT",
            "IMMEDIATEORIGIN", "IMMEDIATEDESTINATION", "ORIGINATINGDFI", "RECEIVINGDFI", "BATCHNUMBER"
        }.Any(token => normalized.Contains(token, StringComparison.Ordinal));
    }

    private object? ResolveOfficialRawValue(string recordCode, object record, CfgLayoutField field)
    {
        var source = field.SourceDefinition;
        var sourceType = source.DataSourceType.Code;
        if (string.Equals(sourceType, "CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            return source.ConstantValue ?? string.Empty;
        }

        if (string.Equals(sourceType, "ENTIDAD", StringComparison.OrdinalIgnoreCase))
        {
            if (TryResolveOfficialValue(record, source.PropertyPath, out var raw))
            {
                return raw;
            }

            throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"No se encontró sourceFieldPath {source.PropertyPath} para campo {field.FieldCode} en RecordCode={recordCode}.");
        }

        if (string.Equals(sourceType, "EXPRESION", StringComparison.OrdinalIgnoreCase))
        {
            var calculationType = ResolveCalculationType(source.ExpressionDsl);
            if (string.Equals(calculationType, "Filler", StringComparison.OrdinalIgnoreCase))
            {
                return new string(' ', field.Length);
            }

            if (string.Equals(calculationType, "JulianSettlementDate", StringComparison.OrdinalIgnoreCase)
                && TryResolveOfficialValue(record, "EffectiveEntryDate", out var effectiveEntryDate))
            {
                var date = effectiveEntryDate switch
                {
                    DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                    DateTime dateTime => dateTime,
                    _ => Convert.ToDateTime(effectiveEntryDate, CultureInfo.InvariantCulture)
                };
                return date.DayOfYear.ToString("D3", CultureInfo.InvariantCulture);
            }

            if (TryResolveOfficialValue(record, calculationType, out var raw))
            {
                return raw;
            }

            throw new NachaGenerationException("NACHA_CALCULATION_FAILED", $"No se pudo resolver cálculo {calculationType} para campo {field.FieldCode} en RecordCode={recordCode}.");
        }

        throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"SourceType {sourceType} no soportado en modo oficial para campo {field.FieldCode}.");
    }

    private static string ResolveCalculationType(string? expressionDsl)
    {
        try
        {
            using var document = JsonDocument.Parse(expressionDsl ?? "{}");
            if (document.RootElement.TryGetProperty("calculationType", out var property))
            {
                var value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch (JsonException)
        {
            throw new NachaGenerationException("NACHA_CALCULATION_FAILED", "ExpressionDsl inválido; detalle interno omitido.");
        }

        throw new NachaGenerationException("NACHA_CALCULATION_FAILED", "ExpressionDsl no declara calculationType.");
    }

    private static string? TryResolveCalculationType(string? expressionDsl)
    {
        try
        {
            using var document = JsonDocument.Parse(expressionDsl ?? "{}");
            return document.RootElement.TryGetProperty("calculationType", out var property)
                ? property.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryResolveOfficialValue(object record, string? path, out object? raw)
    {
        raw = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (record is IReadOnlyDictionary<string, object?> values)
        {
            return TryResolveValue(values, path, out raw)
                   || TryResolveValue(values, ResolveOfficialAlias(path), out raw);
        }

        var property = ResolveProperty(record.GetType(), path)
                       ?? ResolveProperty(record.GetType(), ResolveOfficialAlias(path));
        if (property is null)
        {
            return false;
        }

        raw = property.GetValue(record);
        return true;
    }

    private static string ResolveOfficialAlias(string path)
    {
        return NormalizeIdentifier(path) switch
        {
            "ORIGINATINGDFI" => "OriginatingDFI",
            "ORIGINATINGDFIID" => "OriginatingDFI",
            "ORIGINATINGDFICODE" => "OriginatingDFI",
            "ORIGINATINGDFINUMBER" => "OriginatingDFI",
            "RECEIVERCUSTOMERCODE" => "RecipientIdNumber",
            "INDIVIDUALNAME" => "ReceiverName",
            "ADDENDARECORDINDICATOR" => "AddendumIndicator",
            "PAYMENTRELATEDINFORMATION" => "Purpose",
            _ => path
        };
    }

    private static string FormatOfficialValue(object? raw, CfgLayoutField field)
    {
        if (raw is null)
        {
            return string.Empty;
        }

        if (raw is string text)
        {
            return text;
        }

        if (raw is DateTime date)
        {
            return date.ToString(field.FormatMask ?? "yyyyMMdd", CultureInfo.InvariantCulture);
        }

        if (raw is DateOnly dateOnly)
        {
            return dateOnly.ToString(field.FormatMask ?? "yyyyMMdd", CultureInfo.InvariantCulture);
        }

        if (raw is decimal decimalValue)
        {
            return FormatAmountInCents(decimalValue, field);
        }

        if (raw is double doubleValue)
        {
            if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
            {
                throw BuildRuleException("NACHA_FIELD_TYPE_INVALID", ResolveRuleId(field), "ACH", field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "Monto no finito.");
            }

            return FormatAmountInCents((decimal)doubleValue, field);
        }

        if (raw is float floatValue)
        {
            if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
            {
                throw BuildRuleException("NACHA_FIELD_TYPE_INVALID", ResolveRuleId(field), "ACH", field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "Monto no finito.");
            }

            return FormatAmountInCents((decimal)floatValue, field);
        }

        if (raw is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return raw.ToString() ?? string.Empty;
    }

    private static string FormatAmountInCents(decimal amount, CfgLayoutField field)
    {
        decimal cents;
        try
        {
            cents = checked(amount * 100m);
        }
        catch (OverflowException)
        {
            throw BuildRuleException("NACHA_FIELD_LENGTH_INVALID", ResolveRuleId(field), "ACH", field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "Overflow monetario.");
        }

        if (amount < 0m || cents != decimal.Truncate(cents))
        {
            throw BuildRuleException("NACHA_FIELD_TYPE_INVALID", ResolveRuleId(field), "ACH", field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "El monto debe ser no negativo y expresable en centavos exactos.");
        }

        try
        {
            return checked((long)cents).ToString(CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            throw BuildRuleException("NACHA_FIELD_LENGTH_INVALID", ResolveRuleId(field), "ACH", field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "Overflow monetario.");
        }
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

        var flow = NachaProfileDimensionResolver.ResolveFlowCode(context.Transactions);
        var direction = NachaProfileDimensionResolver.ResolveDirectionCode(context.Transactions);
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
            audit.Trace.Add($"R6:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:VALUES=REDACTED:TF={transforms}:RULES={issuesText}:FB={trace.FallbackStrategy}");
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
            audit.Trace.Add($"R{recordCode}:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:VALUES=REDACTED:TF={transforms}:FB={trace.FallbackStrategy}");
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
                    diffs.Add($"pos={i + 1}");
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

    private async Task PersistGenerationAuditAsync(
        NachaGenerationAuditResult audit,
        int? profileId,
        CancellationToken ct,
        DateTime? capturedAtUtc = null)
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
                ChangedAtUtc = capturedAtUtc ?? DateTime.UtcNow,
                ChangedBy = "system-runtime",
                CorrelationId = $"NACHA-GEN-{(capturedAtUtc ?? DateTime.UtcNow):yyyyMMddHHmmss}"
            });
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                "NACHA_GENERATION_AUDIT_PERSIST_FAILED ErrorType={ErrorType}",
                ex.GetType().Name);
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

        public static FileHeaderRecord From(
            AchCycle cycle,
            IReadOnlyCollection<AchTransaction> transactions,
            NachaHeader? header,
            OperationalTimeSnapshot operationalSnapshot,
            string? fileIdModifier = null)
        {
            var generationTimestamp = ResolveGenerationTimestamp(header, operationalSnapshot.BogotaTimestamp);
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
                FileCreationDate = generationTimestamp,
                FileCreationTime = generationTimestamp,
                FileIdModifier = CoalesceNonEmpty(fileIdModifier, header?.FileIdModifier, "A"),
                RecordSize = string.IsNullOrWhiteSpace(header?.RecordSize) ? "106" : header!.RecordSize,
                BlockingFactor = string.IsNullOrWhiteSpace(header?.BlockingFactor) ? "10" : header!.BlockingFactor,
                FormatCode = string.IsNullOrWhiteSpace(header?.FormatCode) ? "1" : header!.FormatCode,
                ImmediateDestinationName = destinationName,
                ImmediateOriginName = originName,
                ReferenceCode = CoalesceNonEmpty(header?.ReferenceCode),
                CycleName = cycle.CycleName,
                ProcessingDate = operationalSnapshot.BogotaTimestamp
            };
        }

        public static FileHeaderRecord From(
            AchCycle cycle,
            IReadOnlyCollection<AchTransaction> transactions,
            NachaHeader? header,
            string? fileIdModifier = null)
        {
            var legacyTimestamp = NachaFileSnapshotTimeResolver.Resolve(cycle);
            var snapshot = new OperationalTimeSnapshot(
                legacyTimestamp.ToUniversalTime(),
                DateTime.SpecifyKind(legacyTimestamp, DateTimeKind.Unspecified),
                DateOnly.FromDateTime(cycle.ProcessingDate),
                OperationalTimeSnapshotProvider.IanaTimeZoneId);
            return From(cycle, transactions, header, snapshot, fileIdModifier);
        }

        private static string? CoalesceNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        }

        private static DateTime ResolveGenerationTimestamp(NachaHeader? header, DateTime fallback)
        {
            var date = fallback.Date;
            var time = fallback.TimeOfDay;

            if (!string.IsNullOrWhiteSpace(header?.FileCreationDate))
            {
                if (!DateTime.TryParseExact(
                        header.FileCreationDate.Trim(),
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedDate))
                {
                    throw new NachaGenerationException(
                        "NACHA_FIELD_RULE_FAILED",
                        "La fecha de creación no cumple el formato configurado.",
                        ruleId: "ACHCOL-T1-FILE-CREATION-DATE",
                        chamber: "ACHCOL",
                        recordType: "1",
                        fieldName: "FILECREATIONDATE",
                        cause: "Formato distinto de yyyyMMdd.",
                        startPosition: 24,
                        expectedLength: 8);
                }

                date = parsedDate.Date;
            }

            if (!string.IsNullOrWhiteSpace(header?.FileCreationTime))
            {
                if (!DateTime.TryParseExact(
                        header.FileCreationTime.Trim(),
                        "HHmm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedTime))
                {
                    throw new NachaGenerationException(
                        "NACHA_FIELD_RULE_FAILED",
                        "La hora de creación no cumple el formato configurado.",
                        ruleId: "ACHCOL-T1-FILE-CREATION-TIME",
                        chamber: "ACHCOL",
                        recordType: "1",
                        fieldName: "FILECREATIONTIME",
                        cause: "Formato distinto de HHmm.",
                        startPosition: 32,
                        expectedLength: 4);
                }

                time = parsedTime.TimeOfDay;
            }

            return DateTime.SpecifyKind(date.Add(time), fallback.Kind);
        }
    }

    private sealed record CompanyEntryDescriptionCatalogItem(string Term, string StandardEntryClassCode);
    private sealed record BatchCalculation(
        IReadOnlyList<AchTransaction> Transactions,
        string StandardEntryClassCode,
        string BatchEntryDescription,
        int EntryAddendaCount = 0,
        int AddendaOnlyCount = 0,
        long BatchDebit = 0,
        long BatchCredit = 0);

    private sealed record ReceiverLookup(
        IReadOnlyDictionary<(string Document, string Account), IReadOnlyList<Customer>> CustomersByDocumentAndAccount,
        IReadOnlyDictionary<string, List<Customer>> CustomersByAccount)
    {
        public static readonly ReceiverLookup Empty = new(
            new Dictionary<(string Document, string Account), IReadOnlyList<Customer>>(),
            new Dictionary<string, List<Customer>>(StringComparer.Ordinal));
    }

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

        public static BatchHeaderRecord From(
            AchBatch batch,
            string standardEntryClassCode,
            int batchNumber,
            string companyEntryDescription,
            OperationalTimeSnapshot operationalSnapshot)
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
                CompanyDescriptiveDate = batch.EffectiveEntryDate == default ? operationalSnapshot.BogotaTimestamp : batch.EffectiveEntryDate,
                EffectiveEntryDate = batch.EffectiveEntryDate == default ? operationalSnapshot.BogotaTimestamp : batch.EffectiveEntryDate,
                SettlementDate = string.Empty,
                OriginatorStatusCode = "1",
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }

        public static BatchHeaderRecord From(
            AchBatch batch,
            string standardEntryClassCode,
            int batchNumber,
            string companyEntryDescription)
        {
            var fallback = batch.EffectiveEntryDate == default
                ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified)
                : DateTime.SpecifyKind(batch.EffectiveEntryDate, DateTimeKind.Unspecified);
            var snapshot = new OperationalTimeSnapshot(
                DateTime.SpecifyKind(fallback, DateTimeKind.Utc),
                fallback,
                DateOnly.FromDateTime(fallback),
                OperationalTimeSnapshotProvider.IanaTimeZoneId);
            return From(batch, standardEntryClassCode, batchNumber, companyEntryDescription, snapshot);
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
        public int TransactionId { get; init; }
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

        public static EntryDetailRecord From(AchTransaction tx, string receiverName, int receivingDfiLength, bool hasAddenda = true)
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
                TransactionId = tx.Id,
                TransactionCode = tx.TransactionCode,
                ReceivingDFI = receivingDfi,
                CheckDigit = checkDigit,
                DestinationAccountNumber = tx.DestinationAccountNumber,
                Amount = tx.Amount,
                RecipientIdNumber = tx.RecipientIdNumber,
                ReceiverName = receiverName,
                DiscretionaryData = tx.DiscretionaryData,
                AddendumIndicator = hasAddenda ? "1" : "0",
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
            if (string.IsNullOrWhiteSpace(receiverName))
            {
                receiverName = !string.IsNullOrWhiteSpace(transaction.RecipientIdNumber)
                    ? transaction.RecipientIdNumber
                    : $"USUARIO {transaction.Id}";
            }

            var normalizedReceiverName = NachaReceiverNameHelper.SanitizeForType6(receiverName);

            if (string.IsNullOrWhiteSpace(normalizedReceiverName))
            {
                throw new InvalidOperationException("Error Fatal ID 22: el flujo legacy de desarrollo no pudo resolver el nombre del receptor.");
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

        return string.Empty;
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

        public static BatchControlRecord From(AchBatch batch, NachaBatchControlTotals totals, int batchNumber)
        {
            return new BatchControlRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                EntryAddendaCount = totals.EntryAddendaCount,
                EntryHash = totals.EntryHash,
                TotalDebitAmount = totals.TotalDebitAmountInCents,
                TotalCreditAmount = totals.TotalCreditAmountInCents,
                CompanyIdentification = batch.CompanyIdentification,
                MessageAuthenticationCode = string.Empty,
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }

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

        public static FileControlRecord From(AchCycle cycle, NachaFileControlTotals totals)
        {
            return new FileControlRecord
            {
                BatchCount = totals.BatchCount,
                BlockCount = totals.BlockCount,
                EntryAddendaCount = totals.EntryAddendaCount,
                EntryHash = totals.EntryHash,
                TotalDebitAmount = totals.TotalDebitAmountInCents,
                TotalCreditAmount = totals.TotalCreditAmountInCents,
                CycleName = cycle.CycleName
            };
        }

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

            if (long.TryParse(dfi, out var value))
            {
                hash = (hash + value) % maxHash;
            }
        }

        return hash;
    }
}
